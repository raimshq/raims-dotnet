# Raims .NET SDK

Read the README and the contribution and maintenance guides before changing the SDK.
This repository consumes only the published public contract and does not import
code from Raims hosts.

- Keep repository descriptions and all Markdown documentation in English.
  Preserve public identifiers, contract values, commands, and link destinations.
- Do not version or publish internal TODO lists, backlogs, or work plans,
  regardless of their name, format, or directory. Track tasks in an internal
  source outside GitHub. Do not include pending work or private paths in
  public documentation.
- The public namespace is `Raims`; the NuGet package is `Raims`; targets are
  `netstandard2.0` and `net10.0`. Builds and the solution use Release/x64,
  following the workspace convention.
- The pinned contract and its provenance are in `contracts`. Offline generation
  must verify SHA-256 and cover each published operation exactly once.
- Services, options, models, constants, and the reference derived from the
  contract are generated. Do not edit these files manually.
- Preserve JSON names, integer cents, Unix dates, omission, and null.
  Do not generate idempotency keys or retry writes automatically. `202`
  responses retain their meaning of pending processing.
- Authenticated transport does not follow redirects, download external files,
  or upload files. Credentials are isolated per client and request.
- Diagnostics expose only the method, path without IDs, attempt, duration,
  status, and Request-Id. Never include credentials, query parameters, or bodies.
- Tests, simulators, validation consumer projects, and evidence belong
  exclusively in `C:\server\raims\.tmp\raims-dotnet-sdk`.
- Describe Raims features directly in repository text. Preserve provenance
  and mandatory notices for any incorporated code.
- Leave local changes uncommitted and unstaged. GitHub, NuGet, releases, and
  deployment each require their own authorization.
- The `validate.yml` workflow validates branch pushes from this repository
  and manual runs, with required jobs `validateWindows` and `securityLinux`.
  It does not publish packages or receive pull request or fork events.
- The `nuget-release.yml` workflow publishes versioned releases after validating
  the package on authorized CodeBuild runners. Do not publish on each main
  commit or use GitHub-hosted runners or personal computers.
- Before a release, both jobs in `validate.yml` must have completed successfully
  for the same commit. Skipped jobs do not approve a delivery. Preserve the
  Actions lookup, deadlines, and run and attempt linkage in the manifest;
  check them again before requesting the NuGet credential.
- CI uses the canonical Developer Toolkit validator at a pinned revision.
  Publication consumes only the approved package and manifest, with OIDC
  restricted to the publication job and the `release` environment.
