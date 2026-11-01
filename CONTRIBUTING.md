# Contributing

Read the [maintenance guide](docs/maintenance.md) and the instructions in
`AGENTS.md`. Report issues with a minimal reproduction, without credentials,
personal data, or resource access URLs.

## Generation and build

The SDK uses .NET SDK 10 and Python 3.8 or later for offline generation.
From the checkout root:

```powershell
python scripts/generate.py --check
dotnet restore Raims.sln --locked-mode -p:Platform=x64
dotnet build Raims.sln -c Release -p:Platform=x64 --no-restore
```

To update the contract, follow the maintenance procedure and run the generator
without `--check`. Do not edit generated models, options, services, or the
reference. Review the entire diff, including new files and nullability changes.

## Full validation

```powershell
.\scripts\check.ps1
```

The command calls the canonical Developer Toolkit validator in the neighboring
checkout. It materializes consumers and tests only in `.tmp`, checks the
contract, builds the solution, runs consumers, and inspects the package.
Scenarios use fake transport and loopback; they do not call the real API.

The gate also runs the documented installation with an empty cache, checks
descriptions in the XML and reference, and uses Git to export a temporary tree
without a commit. The resulting ZIP must support generation, build, and
packaging, with valid local guide links.

A standalone clone without the Toolkit reports that missing dependency and
does not declare partial validation successful. Generation and build remain
available in the SDK.

In the Raims workspace, tests, simulators, validation consumers, packages, and
reports belong exclusively in `C:\server\raims\.tmp\raims-dotnet-sdk`.
Do not include them in this repository or distribution archives. Stop all
temporary processes and check their ports before finishing.

## Compatibility and distribution

Preserve public signatures, types, JSON names, omission, null, dates, and monetary
values. Do not add idempotency key generation or write retries. The package must
contain assemblies and documentation for both targets, the license, and
integration guides. Engineering contracts, the generator, tests, and evidence
stay outside the NuGet package.

Development tasks and pending work are tracked in an internal source outside
GitHub.

The source ZIP includes the contracts, generator, and guides needed to
generate, build, and package the library. `export-ignore` rules must not remove
a file required by the project or guide links.

Describe Raims features directly in text and examples. Preserve mandatory
license notices and the provenance of incorporated code.

The `validate.yml` workflow runs the same canonical gate on branch pushes from
this repository or manual runs. `validateWindows` checks the targets and
package; `securityLinux` runs security analysis through the canonical Toolkit
helper. Both use authorized CodeBuild runners. This validation does not publish
to NuGet or run on pull requests or forks.

Before publishing a release, check a completed `validate.yml` run for the chosen
commit. Every job must have a `success` result before the release publication
date. `skipped`, failure, or validation of another commit blocks publication.
The release flow checks this history and records the run and attempt in the
package manifest.

Development sources, CI, releases, and NuGet remain separate stages. Successful
validation does not authorize a commit, push, release creation, or publication.
