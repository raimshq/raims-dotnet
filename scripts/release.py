#!/usr/bin/env python3
"""Bind a validated NuGet artifact to its GitHub release and publish that artifact."""

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET
import zipfile


REPOSITORY = "raimshq/raims-dotnet"
PACKAGE_ID = "Raims"
SOURCE = "https://api.nuget.org/v3/index.json"
VERSION = re.compile(r"(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?")
SHA = re.compile(r"[0-9a-f]{40}")
sys.dont_write_bytecode = True


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def git(source, *args):
    process = subprocess.run(["git", "-C", str(source), *args], capture_output=True, text=True)
    require(process.returncode == 0, "The release source could not be verified with Git.")
    return process.stdout.strip()


def identity(source):
    require(os.environ.get("GITHUB_REPOSITORY") == REPOSITORY, "Unexpected release repository.")
    require(os.environ.get("GITHUB_EVENT_NAME") == "release", "Only published releases can publish a package.")
    event = json.loads(Path(os.environ["GITHUB_EVENT_PATH"]).read_text(encoding="utf-8"))
    release = event.get("release", {})
    require(event.get("action") == "published" and release.get("draft") is False, "The release has not been published.")
    require(event.get("repository", {}).get("full_name") == REPOSITORY, "The event repository differs from the workflow.")
    tag = release.get("tag_name", "")
    require(tag.startswith("v") and VERSION.fullmatch(tag[1:]), "Use a versioned release tag such as v0.1.0.")
    revision = os.environ.get("GITHUB_SHA", "")
    require(SHA.fullmatch(revision), "The release revision must be an immutable commit SHA.")
    require(git(source, "rev-parse", "HEAD") == revision, "The checkout differs from the release commit.")
    require(git(source, "rev-parse", "refs/tags/" + tag + "^{commit}") == revision, "The release tag differs from the event commit.")
    require(git(source, "status", "--porcelain") == "", "The release checkout contains local source changes.")
    ancestry = subprocess.run(["git", "-C", str(source), "merge-base", "--is-ancestor", revision, "origin/main"], capture_output=True)
    require(ancestry.returncode == 0, "The release commit must belong to main.")
    project = ET.parse(source / "src/Raims/Raims.csproj")
    require(project.findtext("./PropertyGroup/PackageId") == PACKAGE_ID, "The package ID differs from the approved package.")
    version = project.findtext("./PropertyGroup/Version")
    require(version == tag[1:], "The release tag and project version differ.")
    client = (source / "src/Raims/RaimsClient.cs").read_text(encoding="utf-8-sig")
    match = re.search(r'public const string Version = "([^\"]+)";', client)
    require(match is not None and match.group(1) == version, "The public client and package versions differ.")
    toolkit = os.environ.get("RAIMS_DOTNET_TOOLKIT_REF", "")
    require(SHA.fullmatch(toolkit), "Pin the reviewed Developer Toolkit commit before publishing this workflow.")
    result = {"repository": REPOSITORY, "packageId": PACKAGE_ID, "version": version,
              "tag": tag, "revision": revision, "toolkitRevision": toolkit}
    for field, variable in (("runId", "GITHUB_RUN_ID"), ("runAttempt", "GITHUB_RUN_ATTEMPT")):
        value = os.environ.get(variable, "")
        require(re.fullmatch(r"[1-9][0-9]*", value), "The workflow run identity is missing.")
        result[field] = value
    require(type(release.get("id")) is int and release["id"] > 0, "The GitHub release identity is missing.")
    result["releaseId"] = release["id"]
    return result


def package_metadata(package, expected):
    require(package.is_file() and package.name == PACKAGE_ID + "." + expected["version"] + ".nupkg", "Unexpected package filename.")
    with zipfile.ZipFile(package) as archive:
        manifests = [name for name in archive.namelist() if name.endswith(".nuspec")]
        require(len(manifests) == 1, "The package must contain exactly one manifest.")
        document = ET.fromstring(archive.read(manifests[0]))
        metadata = next((node for node in document if node.tag.rsplit("}", 1)[-1] == "metadata"), None)
        require(metadata is not None, "The package metadata is missing.")
        values = {node.tag.rsplit("}", 1)[-1]: node.text for node in metadata}
        require(values.get("id") == PACKAGE_ID and values.get("version") == expected["version"], "The package identity differs from the release.")


def bundle(source, report_path, destination):
    expected = identity(source)
    toolkit = source.parent / "Developer-Toolkit"
    require(git(toolkit, "rev-parse", "HEAD") == expected["toolkitRevision"], "The validation toolkit differs from the pinned revision.")
    require(git(toolkit, "status", "--porcelain") == "", "The pinned validation toolkit contains local changes.")
    report = json.loads(report_path.read_text(encoding="utf-8"))
    require(report.get("status") == "passed" and report.get("liveApiCalled") is False, "The canonical SDK validation did not pass.")
    require(Path(report.get("source", "")).resolve() == source.resolve(), "The validation report belongs to another checkout.")
    require(report.get("toolingSha256") == digest(toolkit / "scripts/release-preflight/dotnet_sdk.py"), "The validator changed after the check.")
    for group in ("consumers", "installedConsumers"):
        require(set(report.get(group, {})) == {"net48", "net10.0"}, "The canonical report does not cover both supported consumers.")
        require(all(row.get("status") == "passed" for row in report[group].values()), "A supported consumer failed validation.")
    packages = list((report_path.parent / "dist").glob("*.nupkg"))
    require(len(packages) == 1, "The canonical check must produce exactly one package.")
    package = packages[0]
    package_metadata(package, expected)
    require(report.get("packageSha256") == digest(package), "The package changed after validation.")
    require(not destination.exists() or not any(destination.iterdir()), "Use an empty release artifact directory.")
    destination.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(package, destination / package.name)
    manifest = {"schemaVersion": 1, **expected, "packageFile": package.name,
                "packageSha256": digest(package), "validationReportSha256": digest(report_path)}
    (destination / "release-manifest.json").write_text(json.dumps(manifest, sort_keys=True, indent=2) + "\n", encoding="utf-8")
    print("Validated release artifact: " + package.name)


def verify(source, directory):
    expected = identity(source)
    manifest_path = directory / "release-manifest.json"
    require(manifest_path.is_file(), "The validated release manifest is missing.")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    require(manifest.get("schemaVersion") == 1, "Unsupported release manifest.")
    require(all(manifest.get(key) == value for key, value in expected.items()), "The artifact belongs to another release, commit or workflow attempt.")
    filename = PACKAGE_ID + "." + expected["version"] + ".nupkg"
    require(manifest.get("packageFile") == filename, "The artifact package filename differs from the release.")
    require({path.name for path in directory.iterdir()} == {filename, "release-manifest.json"}, "The artifact contains files outside the approved bundle.")
    package = directory / filename
    require(re.fullmatch(r"[0-9a-f]{64}", manifest.get("packageSha256", "")), "The package checksum is invalid.")
    require(digest(package) == manifest["packageSha256"], "The downloaded package checksum differs from the validated artifact.")
    package_metadata(package, expected)
    return package


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("context", "bundle", "verify", "publish"))
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--report", type=Path)
    parser.add_argument("--artifact", type=Path)
    args = parser.parse_args()
    source = args.source.resolve()
    if args.command == "context":
        context = identity(source)
        output = os.environ.get("GITHUB_OUTPUT")
        require(output, "GitHub step outputs are unavailable.")
        with Path(output).open("a", encoding="utf-8") as stream:
            for field in ("version", "revision", "toolkitRevision"):
                stream.write(field + "=" + context[field] + "\n")
        print("Release context verified: " + context["tag"])
    elif args.command == "bundle":
        require(args.report is not None and args.artifact is not None, "Bundle requires the canonical report and artifact directory.")
        bundle(source, args.report.resolve(), args.artifact.resolve())
    else:
        require(args.artifact is not None, "The artifact directory is required.")
        package = verify(source, args.artifact.resolve())
        if args.command == "verify":
            print("Release artifact verified: " + package.name)
        else:
            require(os.environ.get("NUGET_API_KEY"), "NuGet trusted publishing did not supply a temporary credential.")
            key = os.environ["NUGET_API_KEY"]
            dotnet = shutil.which("dotnet")
            require(dotnet, "The .NET CLI is unavailable.")
            process = subprocess.run([dotnet, "nuget", "push", str(package), "--api-key", key, "--source", SOURCE], capture_output=True, text=True)
            output = (process.stdout + process.stderr).replace(key, "[redacted]")
            print(output, end="")
            require(process.returncode == 0, "NuGet rejected the package publication. Inspect its state before retrying.")


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, RuntimeError, zipfile.BadZipFile, ET.ParseError) as error:
        print("Release check failed: " + str(error), file=sys.stderr)
        sys.exit(1)
