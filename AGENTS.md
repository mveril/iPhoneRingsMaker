# Repository Guidelines

## Project Structure & Module Organization

The solution contains three .NET 10 projects. `iPhoneRingsMaker/` is the WinUI 3 desktop application: XAML pages live in `Views/`, presentation logic in `ViewModels/`, and application services in `Services/`. Shared controls, converters, styles, localized resources, images, manifests, and MSIX publishing configuration remain in their corresponding folders under this project.

`iPhoneRingsMaker.Core/` contains reusable models, service contracts, serialization helpers, and project/file management logic. Keep UI-independent code here. `iPhoneRingsMaker.Core.Tests/` contains the xUnit test suite for non-UI behavior.

Resolve dependencies through constructor injection. `App` is the composition root; do not introduce service-locator calls or global window access. Use `IWindowContext` for window-dependent services, `IMediaFactory` for runtime media adapters, `IM4RProjectFactory` for project creation, and dedicated services for conversion and Apple device file access. Keep platform operations out of ViewModels whenever they can be isolated behind a service contract.

## Language and Localization

Use professional US English (`en-US`) for source code, identifiers, comments, documentation, scripts, branch names, commit messages, issues, and pull requests. French is allowed only as localized user-facing content in `Strings/fr-fr/Resources.resw`.

Treat `Strings/en-us/Resources.resw` as the canonical resource catalog. Whenever a user-facing resource is added, removed, or renamed, make the equivalent change in the French catalog and preserve identical resource keys. Do not hardcode user-facing strings in XAML or C#.

## Build, Test, and Development Commands

- `dotnet restore iPhoneRingsMaker.sln` restores all NuGet dependencies.
- `dotnet build iPhoneRingsMaker.sln -c Debug -p:Platform=x64` builds the common local-development configuration.
- `dotnet build iPhoneRingsMaker.sln -c Release -p:Platform=x64` validates a release build.
- `dotnet test iPhoneRingsMaker.sln -c Debug -p:Platform=x64` runs the automated test suite.
- `dotnet format iPhoneRingsMaker.sln --verify-no-changes` checks formatting without rewriting files.

The solution also declares x86 and ARM64 configurations. Run the application through Visual Studio or launch the generated WinUI executable from the relevant `bin/` directory.

## Coding Style & Naming Conventions

Follow `.editorconfig`: use four-space indentation, CRLF line endings, Allman braces, file-scoped namespaces, and sorted `System` imports. Prefer `var` when the type is apparent. Use PascalCase for types, properties, methods, and events; prefix interfaces with `I`. Preserve MVVM boundaries: views own layout, ViewModels own presentation state and commands, and services own reusable operations.

## Testing Guidelines

Add tests for new non-UI behavior in `iPhoneRingsMaker.Core.Tests`. Name tests after the behavior and expected result. Before submitting, run formatting verification, the test suite, and relevant Debug and Release builds. Manually exercise navigation, media selection, trimming, conversion, device discovery, transfer, and Wi-Fi access when changing WinUI or media code.

## Commit & Pull Request Guidelines

Use short, imperative English commit subjects. Pull requests must explain the change, identify affected workflows, link related issues when available, and report build and test results. Include before-and-after screenshots for visible UI changes. Avoid combining package upgrades, broad refactors, and unrelated features in one pull request.

## Open-Source and Security Guidelines

Keep public documentation accurate when behavior, requirements, privacy characteristics, or supported workflows change. Do not commit credentials, tokens, signing certificates, personal paths, device identifiers, user media, generated build output, or private diagnostic data. Follow `SECURITY.md` for vulnerability reports and the MIT license for distribution. Replace placeholder metadata before publishing packages or releases.
