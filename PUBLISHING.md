# Publishing the .NET SDK to NuGet.org

This guide is for MailEngin maintainers. Developers installing the package should use [README.md](./README.md).

Package ID: `MailEngin`  
Repository: `mailengin/mailengin-dotnet-sdk`  
Registry: [NuGet.org](https://www.nuget.org/packages/MailEngin)

## One-Time Account Setup

1. Create a MailEngin-controlled [NuGet.org](https://www.nuget.org/) account.
2. Verify the account email address and enable multifactor authentication.
3. Store recovery information in the company password manager.
4. Confirm `MailEngin` is available or already controlled by MailEngin.
5. Reserve the `MailEngin` package prefix when the organization becomes eligible.

Stop for ownership review if an unrelated publisher controls the package ID.

## Configure Trusted Publishing

In the NuGet.org account, create a GitHub trusted-publishing policy:

| Field | Value |
| --- | --- |
| Repository owner | `mailengin` |
| Repository | `mailengin-dotnet-sdk` |
| Workflow file | `release.yml` |
| Environment | `release` |

Create a protected GitHub environment named `release` and require maintainer approval. Add one environment secret:

| Secret | Value |
| --- | --- |
| `NUGET_USER` | NuGet.org profile username, not an email address or API key |

The workflow uses GitHub OIDC and `NuGet/login@v1` to obtain a short-lived key. Do not create a long-lived NuGet API key for CI.

Before the first release, run the `Release` workflow manually from GitHub Actions. A manual run builds the package and verifies the OIDC login without publishing. Only a `v*` tag push publishes to NuGet.org.

## Prepare a Release

Update `<Version>` in `src/MailEngin/MailEngin.csproj` and add release notes to `CHANGELOG.md`.

```bash
dotnet restore
dotnet test -c Release
dotnet pack src/MailEngin/MailEngin.csproj -c Release -o artifacts
```

The build must produce matching `.nupkg` and `.snupkg` files. Both formats are ZIP archives, so inspect their file lists directly:

```bash
unzip -l artifacts/MailEngin.0.1.0.nupkg
unzip -l artifacts/MailEngin.0.1.0.snupkg
```

On Windows, `tar -tf` can list these archives. You can also open a copied package after changing the copy's extension to `.zip`; do not modify the original artifacts.

Confirm both target frameworks, DLLs, XML metadata where present, README, MIT license, dependencies, repository URL, symbols, and version are correct. Ensure no credentials, `.env` files, customer data, test output, or unrelated source files are included.

## Local Feed Smoke Test

```bash
mkdir mailengin-dotnet-release-check
cd mailengin-dotnet-release-check
dotnet new console --framework net8.0
dotnet nuget add source ../mailengin-dotnet-sdk/artifacts --name mailengin-local
dotnet add package MailEngin --version 0.1.0 --source mailengin-local
dotnet build
```

Compile a small program that constructs `MailEnginClient` with a mocked `HttpClient`. Remove the temporary package source afterward if it was added to a user-level NuGet configuration.

## Tag and Publish

```bash
git add src/MailEngin/MailEngin.csproj CHANGELOG.md
git commit -m "Release .NET SDK v0.1.0"
git push origin main
git tag -a v0.1.0 -m "MailEngin .NET SDK v0.1.0"
git push origin v0.1.0
```

The workflow verifies that the tag matches the project version, runs tests, creates package and symbol artifacts, waits for `release` approval, exchanges GitHub identity for a temporary NuGet key, and pushes the release to NuGet.org.

## Verify the Public Release

NuGet validation and symbol indexing can take several minutes. After the package appears:

```bash
mkdir mailengin-dotnet-public-check
cd mailengin-dotnet-public-check
dotnet new console --framework net8.0
dotnet add package MailEngin --version 0.1.0
dotnet build
```

Verify the NuGet page shows the expected version, README, frameworks, license, repository, dependencies, package contents, and symbols. Repeat installation from a .NET Standard 2.0 consumer when changing multi-targeting or dependencies.

## Release Recovery

NuGet versions are immutable. If a release is dangerous, unlist it and publish a corrected patch. Existing consumers can still restore an unlisted exact version, so document the affected range clearly.

If OIDC login fails, verify the NuGet policy fields, GitHub environment name, workflow filename, `id-token: write` permission, and `NUGET_USER` profile name. These values must match exactly.

Transfer ownership through NuGet package owners. Recreate or audit the trusted-publishing policy after ownership or repository changes and remove stale owners and tokens.

Official references: [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) and [publishing symbol packages](https://learn.microsoft.com/en-us/nuget/create-packages/symbol-packages-snupkg).

See the workspace [release handbook](../PUBLISHING.md) for the shared checklist and incident process.
