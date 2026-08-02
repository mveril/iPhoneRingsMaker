<#
.SYNOPSIS
Builds and launches iPhoneRingsMaker as a packaged WinUI application.

.EXAMPLE
.\BuildAndRun.ps1

.EXAMPLE
.\BuildAndRun.ps1 -Configuration Release -Detach
#>

[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateSet("x64", "x86", "ARM64")]
    [string]$Platform = "x64",

    [switch]$Detach,
    [switch]$SkipRun
)

$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "iPhoneRingsMaker\iPhoneRingsMaker.csproj"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "The .NET SDK was not found in PATH."
}

if (-not $SkipRun -and -not (Get-Command winapp -ErrorAction SilentlyContinue)) {
    throw "WinApp CLI was not found in PATH. Install Microsoft.WinAppCLI first."
}

Write-Host "--> Building iPhoneRingsMaker ($Configuration, $Platform)" -ForegroundColor Cyan
& dotnet build $project -c $Configuration "-p:Platform=$Platform"
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if ($SkipRun) {
    exit 0
}

$binDirectory = Join-Path (Split-Path $project -Parent) "bin\$Platform\$Configuration"
$runtimeIdentifier = "win-$($Platform.ToLowerInvariant())"
$manifest = Get-ChildItem $binDirectory -Recurse -Filter "AppxManifest.xml" -File |
    Where-Object { $_.Directory.Name -eq $runtimeIdentifier } |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if (-not $manifest) {
    throw "AppxManifest.xml was not found under '$binDirectory'."
}

$outputDirectory = $manifest.Directory.FullName
Write-Host "--> Launching packaged application from $outputDirectory" -ForegroundColor Cyan

$runArguments = @("run", $outputDirectory, "--manifest", $manifest.FullName)
if ($Detach) {
    $runArguments += "--detach"
} else {
    $runArguments += "--debug-output"
}

& winapp @runArguments
exit $LASTEXITCODE
