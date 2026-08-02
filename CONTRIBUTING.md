# Contributing

Thank you for helping improve iPhoneRingsMaker.

## Before you start

- Search existing issues before opening a new one.
- Use an issue to discuss substantial features or behavior changes first.
- Report security vulnerabilities privately as described in [SECURITY.md](SECURITY.md).
- Keep each pull request focused on one change.

## Development setup

1. Install the .NET 10 SDK and the Windows application development workload.
2. Fork and clone the repository.
3. Create a branch with a short English name.
4. Restore and build the solution:

   ```powershell
   dotnet restore iPhoneRingsMaker.sln
   dotnet build iPhoneRingsMaker.sln -c Debug -p:Platform=x64
   ```

## Standards

- Use `en-US` English for code, identifiers, comments, documentation, branches, commits, issues, and pull requests.
- Keep user-facing strings in resource files. English is canonical; update the matching French resource whenever a key changes.
- Follow `.editorconfig` and preserve the MVVM boundaries described in `AGENTS.md`.
- Add tests for new non-UI behavior.
- Use short, imperative commit subjects, such as `Add ringtone duration validation`.

## Validation

Run these commands before submitting a pull request:

```powershell
dotnet format iPhoneRingsMaker.sln --verify-no-changes --no-restore
dotnet test iPhoneRingsMaker.sln -c Debug -p:Platform=x64
dotnet build iPhoneRingsMaker.sln -c Release -p:Platform=x64
```

Manually exercise affected WinUI workflows. Include before-and-after screenshots for visible changes and report all validation results in the pull request.
