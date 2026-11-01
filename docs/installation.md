# Installation

Development sources are in
[raimshq/raims-dotnet](https://github.com/raimshq/raims-dotnet), on the `main`
branch. The public package is [`Raims`](https://www.nuget.org/packages/Raims),
with targets for x64 applications on .NET Framework 4.8/4.8.1 and .NET 10.

## Install from NuGet

From the consuming application's directory:

```powershell
dotnet add package Raims --version 0.1.5
```

Keep the version under your application's control. Review each release's notes
before upgrading and test the integration on the frameworks your project uses.
The SDK version is independent of the API's `/v1` HTTP version.

## Use the sources

Download the sources and build the solution with .NET SDK 10:

```powershell
git clone https://github.com/raimshq/raims-dotnet.git Raims-Dotnet
cd Raims-Dotnet
dotnet build Raims.sln -c Release -p:Platform=x64
```

Then add a project reference from your application's directory:

```powershell
dotnet add reference ..\Raims-Dotnet\src\Raims\Raims.csproj
```

The example assumes neighboring application and checkout directories; adjust
the path to your layout. Build the application for x64. The SDK solution uses
Release/x64 by default. Keep the selected revision under your application's
control; the `main` branch may receive new development versions.

## Use a local package

From the checkout root, create the package in a local artifact directory:

```powershell
dotnet pack Raims.sln -c Release -p:Platform=x64 -o C:\server\raims\.tmp\raims-dotnet-sdk\packages
```

Add the package directory and official feed to the application's `NuGet.Config`,
preserving its other settings. Adjust the path to the directory where the
package was created. For a new project, the file may contain:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="raims-local" value="C:\server\raims\.tmp\raims-dotnet-sdk\packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

From the consuming application's directory, where this file was saved:

```powershell
dotnet add package Raims --version 0.1.5 --no-restore
dotnet restore --configfile NuGet.Config
```

The first command adds the reference without trying to download the package.
Restore uses the sources in the file: the local directory for the SDK and the
official NuGet feed for its dependencies. Keep both sources, including on
machines with an empty cache. These commands do not publish the SDK.

## Compatibility

| x64 application | Target selected by NuGet |
| --- | --- |
| .NET Framework 4.8 or 4.8.1 on Windows | `netstandard2.0` |
| .NET 10 | `net10.0` |

Validation consumers run on .NET Framework 4.8 and .NET 10. Other implementations
compatible with .NET Standard 2.0 may resolve that target; this alone does not
mean support for them has been validated.

The SDK uses `System.Text.Json`. The Standard target also includes compatible
asynchronous interfaces. In .NET Framework applications, allow binding redirect
generation and install runtime and operating system updates. Do not disable
certificate verification to work around TLS errors.

Sandbox and Production keys use `https://api.raims.com`. The key determines the
environment, company, and permissions; simulation methods require a Sandbox key.
