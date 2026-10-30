#!/usr/bin/env python3
"""Bind a validated NuGet artifact to its GitHub release and publish that artifact."""

import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import urllib.error
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET
import zipfile


REPOSITORY = "raimshq/raims-dotnet"
PACKAGE_ID = "Raims"
SOURCE = "https://api.nuget.org/v3/index.json"
VERSION = re.compile(r"(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?")
SHA = re.compile(r"[0-9a-f]{40}")
VALIDATION_WORKFLOW = ".github/workflows/validate.yml"
VALIDATION_JOBS = {
    "validateWindows": {"image:windows-ec2-2022", "instance-size:medium", "dotnet-sdk-check"},
    "securityLinux": {"dotnet-sdk-security"},
}
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


def timestamp(value):
    require(isinstance(value, str) and value.endswith("Z"), "A release or validation timestamp is missing.")
    try:
        parsed = datetime.fromisoformat(value[:-1] + "+00:00")
    except ValueError:
        raise RuntimeError("A release or validation timestamp is invalid.") from None
    require(parsed.tzinfo is not None, "Validation timestamps must identify UTC.")
    return parsed.astimezone(timezone.utc)


def positive_id(value):
    require(type(value) in (int, str) and re.fullmatch(r"[1-9][0-9]*", str(value)), "The validation run identity is invalid.")
    return str(value)


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, fp, code, message, headers, new_url):
        return None


def github_api(path):
    require(path.startswith("/") and not path.startswith("//"), "Invalid GitHub API request.")
    token = os.environ.get("GITHUB_TOKEN")
    require(token, "The read-only GitHub Actions token is unavailable.")
    request = urllib.request.Request("https://api.github.com/repos/" + REPOSITORY + path, headers={
        "Authorization": "Bearer " + token, "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2026-03-10", "User-Agent": "RaimsSDKRelease",
    })
    try:
        with urllib.request.build_opener(NoRedirect()).open(request, timeout=20) as response:
            raw = response.read(2_000_001)
        require(len(raw) <= 2_000_000, "The GitHub validation response exceeds its limit.")
        result = json.loads(raw.decode("utf-8"))
    except (urllib.error.URLError, TimeoutError, UnicodeError, ValueError):
        raise RuntimeError("The prior validation could not be read from GitHub. Publication remains blocked.") from None
    require(isinstance(result, dict), "The GitHub validation response is invalid.")
    return result


def validation_workflow():
    workflow = github_api("/actions/workflows/validate.yml")
    require(type(workflow.get("id")) is int and workflow["id"] > 0, "The validation workflow identity is missing.")
    require(workflow.get("path") == VALIDATION_WORKFLOW and workflow.get("state") == "active", "The required validation workflow is not active.")
    return workflow["id"]


def attempt_jobs(run_id, attempt):
    result = []
    total = None
    for page in range(1, 21):
        response = github_api("/actions/runs/" + run_id + "/attempts/" + attempt + "/jobs?" + urllib.parse.urlencode({"per_page": 100, "page": page}))
        rows = response.get("jobs")
        require(isinstance(rows, list) and type(response.get("total_count")) is int, "The validation jobs response is incomplete.")
        if total is None:
            total = response["total_count"]
        require(total == response["total_count"] and total >= 0, "The validation job count changed during verification.")
        result.extend(rows)
        if len(result) >= total:
            require(len(result) == total, "The validation jobs response has an unexpected count.")
            return result
        require(len(rows) == 100, "The validation jobs response was truncated.")
    raise RuntimeError("The validation job history exceeds its verification limit.")


def verify_validation_run(run_id, attempt, workflow_id, revision, published_at):
    run_id, attempt = positive_id(run_id), positive_id(attempt)
    run = github_api("/actions/runs/" + run_id + "/attempts/" + attempt)
    require(type(run.get("id")) is int and str(run["id"]) == run_id and str(run.get("run_attempt")) == attempt, "The prior validation run or attempt differs from the requested proof.")
    require(run.get("workflow_id") == workflow_id and run.get("path") == VALIDATION_WORKFLOW, "The prior run belongs to another workflow.")
    require(isinstance(run.get("repository"), dict) and isinstance(run.get("head_repository"), dict) and
            run["repository"].get("full_name") == REPOSITORY and run["head_repository"].get("full_name") == REPOSITORY, "The prior validation did not run against this repository.")
    require(run.get("event") in ("push", "workflow_dispatch"), "Only trusted push or manual validation runs can authorize a release.")
    require(run.get("head_sha") == revision, "The prior validation checked another commit.")
    require(run.get("status") == "completed" and run.get("conclusion") == "success", "The prior validation did not complete successfully.")
    created, started, completed = (timestamp(run.get(key)) for key in ("created_at", "run_started_at", "updated_at"))
    require(created <= started <= completed < published_at, "The validation must finish before the GitHub release is published.")
    jobs = attempt_jobs(run_id, attempt)
    names, identifiers = set(), set()
    route = "codebuild-raims-github-actions-" + run_id + "-" + attempt
    for job in jobs:
        require(isinstance(job, dict) and type(job.get("id")) is int and job["id"] > 0 and job["id"] not in identifiers, "The validation jobs contain a missing or duplicated identity.")
        identifiers.add(job["id"])
        name = job.get("name")
        require(isinstance(name, str) and name and name not in names, "The validation job names are missing or duplicated.")
        names.add(name)
        require(job.get("run_id") == int(run_id) and job.get("head_sha") == revision, "A validation job belongs to another run or commit.")
        require(job.get("status") == "completed" and job.get("conclusion") == "success", "Every validation job must complete with success; skipped jobs cannot authorize publication.")
        job_started, job_completed = timestamp(job.get("started_at")), timestamp(job.get("completed_at"))
        require(started <= job_started <= job_completed <= completed and job_completed < published_at, "A validation job did not finish before the release.")
        labels = job.get("labels")
        require(isinstance(labels, list) and all(isinstance(value, str) for value in labels) and route in labels, "A validation job did not use the required CodeBuild route.")
        require(type(job.get("runner_id")) is int and job["runner_id"] > 0 and isinstance(job.get("runner_name"), str) and job["runner_name"], "The validation runner identity is missing.")
        if name in VALIDATION_JOBS:
            require(VALIDATION_JOBS[name].issubset(labels), "A required validation job used an unexpected runner profile.")
    require(set(VALIDATION_JOBS).issubset(names), "The Windows or security validation job is missing.")
    return {"validationRunId": run_id, "validationRunAttempt": attempt, "validationWorkflowId": workflow_id,
            "validationCompletedAt": completed.isoformat().replace("+00:00", "Z")}


def prior_validation(revision, published_at, run_id=None, attempt=None):
    workflow_id = validation_workflow()
    require((run_id is None) == (attempt is None), "Provide both the validation run ID and attempt.")
    if run_id is not None:
        return verify_validation_run(run_id, attempt, workflow_id, revision, published_at)
    for page in range(1, 11):
        response = github_api("/actions/workflows/" + str(workflow_id) + "/runs?" +
                              urllib.parse.urlencode({"head_sha": revision, "status": "completed", "per_page": 100, "page": page}))
        rows = response.get("workflow_runs")
        require(isinstance(rows, list), "The prior validation history is unavailable.")
        for run in rows:
            if not isinstance(run, dict) or run.get("head_sha") != revision or run.get("conclusion") != "success":
                continue
            try:
                return verify_validation_run(run.get("id"), run.get("run_attempt"), workflow_id, revision, published_at)
            except RuntimeError as error:
                if "could not be read from GitHub" in str(error):
                    raise
        if len(rows) < 100:
            break
    raise RuntimeError("No successful Windows and security validation for this commit finished before the release.")


def validation_context(source):
    require(os.environ.get("GITHUB_REPOSITORY") == REPOSITORY, "Unexpected validation repository.")
    require(os.environ.get("GITHUB_EVENT_NAME") in ("push", "workflow_dispatch"), "Only repository branch pushes and manual runs can validate this source.")
    event = json.loads(Path(os.environ["GITHUB_EVENT_PATH"]).read_text(encoding="utf-8"))
    require(event.get("repository", {}).get("full_name") == REPOSITORY, "The validation event belongs to another repository.")
    require(os.environ.get("GITHUB_REF", "").startswith("refs/heads/"), "Only repository branches can use this validation workflow.")
    revision = os.environ.get("GITHUB_SHA", "")
    require(SHA.fullmatch(revision) and git(source, "rev-parse", "HEAD") == revision, "The validation checkout differs from the event commit.")
    require(git(source, "status", "--porcelain") == "", "The validation checkout contains local source changes.")
    toolkit = os.environ.get("RAIMS_DOTNET_TOOLKIT_REF", "")
    require(SHA.fullmatch(toolkit), "Pin the reviewed Developer Toolkit commit before validation.")
    return {"revision": revision, "toolkitRevision": toolkit}


def identity(source, validation_run_id=None, validation_run_attempt=None):
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
    published_at = timestamp(release.get("published_at"))
    published = github_api("/releases/" + str(release["id"]))
    require(published.get("id") == release["id"] and published.get("tag_name") == tag and published.get("draft") is False and
            published.get("published_at") == release.get("published_at"), "The release event differs from its published GitHub identity.")
    result["releasePublishedAt"] = published_at.isoformat().replace("+00:00", "Z")
    result.update(prior_validation(revision, published_at, validation_run_id, validation_run_attempt))
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


def bundle(source, report_path, destination, validation_run_id, validation_run_attempt):
    expected = identity(source, validation_run_id, validation_run_attempt)
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
    manifest = {"schemaVersion": 2, **expected, "packageFile": package.name,
                "packageSha256": digest(package), "validationReportSha256": digest(report_path)}
    (destination / "release-manifest.json").write_text(json.dumps(manifest, sort_keys=True, indent=2) + "\n", encoding="utf-8")
    print("Validated release artifact: " + package.name)


def verify(source, directory):
    manifest_path = directory / "release-manifest.json"
    require(manifest_path.is_file(), "The validated release manifest is missing.")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    require(manifest.get("schemaVersion") == 2, "Unsupported release manifest.")
    run_id, attempt = positive_id(manifest.get("validationRunId")), positive_id(manifest.get("validationRunAttempt"))
    expected = identity(source, run_id, attempt)
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
    parser.add_argument("command", choices=("validation-context", "context", "bundle", "verify", "publish"))
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--report", type=Path)
    parser.add_argument("--artifact", type=Path)
    parser.add_argument("--validation-run-id")
    parser.add_argument("--validation-run-attempt")
    args = parser.parse_args()
    source = args.source.resolve()
    if args.command in ("context", "validation-context"):
        context = identity(source) if args.command == "context" else validation_context(source)
        output = os.environ.get("GITHUB_OUTPUT")
        require(output, "GitHub step outputs are unavailable.")
        with Path(output).open("a", encoding="utf-8") as stream:
            for field in ("version", "revision", "toolkitRevision", "validationRunId", "validationRunAttempt", "validationWorkflowId", "validationCompletedAt"):
                if field not in context:
                    continue
                stream.write(field + "=" + str(context[field]) + "\n")
        print("Release context verified: " + context["tag"] if args.command == "context" else "Repository validation context verified.")
    elif args.command == "bundle":
        require(args.report is not None and args.artifact is not None, "Bundle requires the canonical report and artifact directory.")
        require(args.validation_run_id is not None and args.validation_run_attempt is not None, "Bundle requires the prior validation run and attempt.")
        bundle(source, args.report.resolve(), args.artifact.resolve(), args.validation_run_id, args.validation_run_attempt)
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
