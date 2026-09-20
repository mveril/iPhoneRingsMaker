# iPhoneRingsMaker

iPhoneRingsMaker is a Windows desktop application for creating M4R ringtones and transferring them to a compatible iPhone, iPad, or iPod.

[![Build](https://github.com/mveril/iPhoneRingsMaker/actions/workflows/build.yml/badge.svg)](https://github.com/mveril/iPhoneRingsMaker/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## Features

- Open or drag an audio file into the application.
- Select an excerpt of up to 30 seconds.
- Export the selection as an M4R ringtone.
- Browse compatible music stored locally on an iPhone.
- Transfer a ringtone directly to a connected Apple device.
- Enable or disable Wi-Fi access for a paired device.
- Use the interface in English or French, based on Windows language settings.

## Requirements

- Windows 10 version 1809 or later
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022 with the Windows application development workload, or the command-line tools
- An x64, x86, or ARM64 Windows device
- A data-capable USB cable for initial iPhone pairing and transfer

The iPhone must be unlocked and must trust the computer. Apple Music tracks protected by DRM and cloud-only tracks cannot be imported.

## Build

```powershell
dotnet restore iPhoneRingsMaker.sln
dotnet build iPhoneRingsMaker.sln -c Debug -p:Platform=x64
dotnet test iPhoneRingsMaker.sln -c Debug -p:Platform=x64
dotnet format iPhoneRingsMaker.sln --verify-no-changes --no-restore
```

To build and launch the packaged application with the WinApp CLI:

```powershell
.\BuildAndRun.ps1
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for the complete development workflow.

## Privacy and security

iPhoneRingsMaker processes media and device information locally. Read the [privacy policy](PRIVACY.md) and report vulnerabilities according to the [security policy](SECURITY.md).

## Disclaimer

iPhone, iPad, iPod, and Apple Music are trademarks of Apple Inc. This project is independent and is not affiliated with, endorsed by, or sponsored by Apple Inc.

## License

Copyright (c) 2026 Mickaël Véril. Released under the [MIT License](LICENSE).
