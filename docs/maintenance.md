# Contract and maintenance

## Source

The SDK consumes the public contract distributed in
[raimshq/openapi](https://github.com/raimshq/openapi). `contracts/source.json`
records the immutable revision, URL, SHA-256, API version, and operation count.
The initial snapshot pins 67 operations from revision
`a412f1f2d12edec7edf001bf2fb4b48403fcec97`.

The generator is offline: it checks the hash and does not download contracts,
call the API, or read credentials. Do not copy endpoints that exist only in
local API code. The published mirror remains the boundary of the distributed
surface.

## Organization

| File or directory | Responsibility |
| --- | --- |
| `src/Raims/RaimsClient.cs` | Service composition and client lifecycle |
| `src/Raims/Services` | Resource methods; manually maintained simulation wrapper |
| `src/Raims/Options` | Typed inputs that distinguish omission from null |
| `src/Raims/Models` | Responses and nested objects |
| `src/Raims/Generated` | HTTP catalog, symbol map, and constants |
| `src/Raims/ApiRequestor.cs` | Serialization, structural validation, retries, and errors |
| `src/Raims/Http` | HTTP transport, cancellation, and timeout |
| `src/Raims/Serialization` | Metadata, collections, and exact integer parsing |
| `src/Raims/RaimsCollection.cs` | Cursor pagination |
| `src/Raims/Exceptions` | Failure categories and metadata |
| `src/Raims/WebhookUtility.cs` | Authentication of delivery bytes |
| `scripts/generate.py` | Deterministic generation |
| `docs/reference.md` | Signatures and fields derived from the contract |
| `docs/reference.en.json` | Reviewed English translations for the generated reference |

All public types use the `Raims` namespace. Options and models are generated;
`RaimsClient`, `BaseService`, the `SimulationsService` wrapper, and runtime are
maintained manually. The service catalog identifies each operation exactly
once, without fallback to arbitrary endpoints.

The SDK uses option classes per operation and `Optional<T>` for each field.
Responses preserve unknown fields and allow future enum codes. Structural
validation enforces types, required fields, and nullability without duplicating
permissions, financial rules, or all of the API's domain validation. Monetary
values and dates use integers; the reader does not convert them through floating
point.

The generated reference uses the reviewed translations in `docs/reference.en.json`.
The generator requires an English translation for every nonempty operation and
input-field description it includes. Contract updates must update that map as
needed. This editorial layer does not change the pinned contract or the XML
documentation derived from it.

## Update the contract

1. Select an approved public revision and verify its manifest and checksum.
2. Update the snapshot and `contracts/source.json` without inventing routes or fields.
3. Review semantics, statuses, idempotency, fields, environments, and pagination.
4. Adjust `contracts/service-map.json` if there are new operations and run
   `python scripts/generate.py`.
5. Review signatures, types, constants, and the reference; run the full validation
   in the [contribution guide](../CONTRIBUTING.md).
6. Update affected guides and record compatibility changes before distribution.
   An incompatible change requires a major version.

`--check` compares expected files without writing them. The generator rejects
a mismatched hash, missing or duplicate operations, and compositions without an
implemented representation. Do not update a compatibility baseline to hide a
breaking change.

The project enables .NET package validation to check included targets. After
the first stable release, establish the published version as that validation's
baseline before another distribution.

## Prepare a version

The package version in `src/Raims/Raims.csproj` and the `RaimsClient.Version`
constant must match. Update installation examples to the version being
distributed as well. The package name remains defined in `PackageId`; the
version does not change the API's `/v1` HTTP version.

Before distribution, check the version, metadata, and resulting package, run
the delivery gates, and validate installation on supported targets. Use
authorized CodeBuild runners for the release flow. A GitHub release alone does
not confirm NuGet availability; check the publication result and installation
of the version from the public registry.

## Validation before release

The `validate.yml` workflow runs on branch pushes from this repository and
manual runs. `validateWindows` uses the canonical gate to build and run consumers
on .NET Framework 4.8 and .NET 10 and check the package. `securityLinux` calls the
canonical Toolkit security helper. The Toolkit revision is pinned in workflows;
missing tools or incomplete analysis fail validation.

Both jobs use CodeBuild runners and must finish with `success` for the same
commit that will be published. Pull requests and forks do not enter this flow.
Validation does not publish packages, create releases, or request NuGet
credentials.

Publish the release only after that run finishes. The `nuget-release.yml`
workflow queries Actions to check the repository, event, commit, run, attempt,
runners, and job completion. A run after release publication does not approve
the delivery retroactively. `skipped` and failures also block publication.

The artifact manifest records the ID, attempt, workflow, and completion of the
earlier validation. The publication job checks this linkage again before
requesting the temporary OIDC credential. After publication, check package
availability and installation from NuGet; upload acceptance may precede registry
indexing.

## Diagnostics and availability

Connection, HTTP, and protocol exceptions and the attempt observer let the
application detect unavailability and invalid responses. Aggregate these
failures and confirm recovery through new valid responses. Do not repeat a
previous write to check health or send telemetry containing personal data.

Sources are on GitHub, and the `Raims` package is available on
[NuGet](https://www.nuget.org/packages/Raims). The
[integration guide](https://docs.raims.com/br/integracoes/sdk-dotnet) and
[publication note](https://docs.raims.com/br/changelog/post/2026-10-29-sdk-dotnet)
are in the Raims documentation. The guide also belongs to the approved support
catalog. Support must distinguish a development checkout from a version
available in the package manager.
