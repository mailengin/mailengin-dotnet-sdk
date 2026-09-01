# Contributing to the MailEngin .NET SDK

Thank you for improving the MailEngin SDK. Contributions should preserve .NET Standard 2.0 compatibility, nullable annotations, asynchronous APIs, and behavior shared by the official SDK suite.

## Before You Start

- Use the .NET 8 SDK or newer.
- Search existing issues and pull requests before opening a duplicate.
- Open an issue before a large public API change.
- Report security vulnerabilities privately according to [SECURITY.md](./SECURITY.md).

## Local Setup

```bash
dotnet restore
dotnet test -c Release
dotnet pack src/MailEngin/MailEngin.csproj -c Release -o artifacts
```

## Making Changes

1. Create a focused branch from the latest `main`.
2. Preserve both target frameworks and nullable correctness.
3. Keep public APIs asynchronous and cancellation-aware.
4. Add xUnit coverage for affected validation, JSON mapping, errors, timeouts, cancellation, or transport behavior.
5. Update the README for any developer-visible behavior.
6. Add a concise entry under `Unreleased` in `CHANGELOG.md`.

Do not add automatic send retries until MailEngin supports idempotency keys.

## Pull Request Checklist

- [ ] Release-mode tests pass for all target frameworks.
- [ ] `dotnet pack` creates valid `.nupkg` and `.snupkg` artifacts.
- [ ] New behavior uses an injected `HttpClient` and needs no real API key.
- [ ] Public model or exception changes are documented.
- [ ] The changelog is updated.
- [ ] No API key, `.env` file, customer data, credential, or production log is included.

Maintainers may request changes to preserve cross-SDK behavior or package compatibility. By contributing, you agree that your work is released under the repository's MIT License.
