param(
    [Parameter(Mandatory)][int]$AppPid,
    [string]$MediaPath
)

$ErrorActionPreference = 'Continue'
$pass = 0
$fail = 0
$results = @()

function Test-UI
{
    param([string]$Name, [scriptblock]$Script)

    try
    {
        $output = & $Script 2>&1
        if ($LASTEXITCODE -eq 0)
        {
            $script:pass++
            $script:results += @{ name = $Name; status = 'PASS' }
        }
        else
        {
            $script:fail++
            $script:results += @{ name = $Name; status = 'FAIL'; detail = "$output" }
        }
    }
    catch
    {
        $script:fail++
        $script:results += @{ name = $Name; status = 'FAIL'; detail = "$_" }
    }
}

Test-UI 'TitleBar exists' { winapp ui wait-for 'AppTitleBar' -a $AppPid -t 5000 }
Test-UI 'Project commands exist' { winapp ui wait-for 'ProjectCommandBar' -a $AppPid -t 5000 }
Test-UI 'Open command exists' { winapp ui wait-for 'OpenFileButton' -a $AppPid -t 5000 }
Test-UI 'Save command exists' { winapp ui wait-for 'SaveFileButton' -a $AppPid -t 5000 }
Test-UI 'Save As command exists' { winapp ui wait-for 'SaveAsFileButton' -a $AppPid -t 5000 }
Test-UI 'Edition navigation exists' { winapp ui wait-for 'EditionNavigationItem' -a $AppPid -t 5000 }
Test-UI 'Conversion navigation exists' { winapp ui wait-for 'ConversionNavigationItem' -a $AppPid -t 5000 }

New-Item -ItemType Directory -Force -Path 'screenshots' | Out-Null
winapp ui screenshot -a $AppPid -o 'screenshots/01-shell.png' 2>$null

Test-UI 'Navigate to Conversion' { winapp ui invoke 'ConversionNavigationItem' -a $AppPid }
Test-UI 'Conversion page loaded' { winapp ui wait-for 'ConversionPageTitle' -a $AppPid -t 5000 }
winapp ui screenshot -a $AppPid -o 'screenshots/02-conversion.png' 2>$null

Test-UI 'Navigate to Edition' { winapp ui invoke 'EditionNavigationItem' -a $AppPid }
Test-UI 'Edition page loaded' { winapp ui wait-for 'EditionPageTitle' -a $AppPid -t 5000 }
winapp ui screenshot -a $AppPid -o 'screenshots/03-edition.png' 2>$null

Test-UI 'Open iPhone music menu' {
    winapp ui focus 'OpenMediaEmptyStateButton' -a $AppPid
    winapp ui send-keys 'alt+down' -a $AppPid --via send-input
}
Test-UI 'Open iPhone music dialog' { winapp ui invoke 'OpenIPhoneMusicEmptyStateButton' -a $AppPid }
Test-UI 'iPhone music dialog loaded' { winapp ui wait-for 'MusicLibraryDeviceSelector' -a $AppPid -t 5000 }
winapp ui screenshot -a $AppPid -o 'screenshots/04-iphone-music-picker.png' 2>$null
Test-UI 'Close iPhone music dialog' { winapp ui invoke 'CloseButton' -a $AppPid }

Test-UI 'Settings button exists' { winapp ui wait-for 'SettingsButton' -a $AppPid -t 5000 }
Test-UI 'Navigate to Settings' { winapp ui invoke 'SettingsButton' -a $AppPid }
Test-UI 'Settings page loaded' { winapp ui wait-for 'SettingsPageTitle' -a $AppPid -t 5000 }
Test-UI 'Theme selector is accessible' { winapp ui wait-for 'ThemeSelector' -a $AppPid -t 5000 }
winapp ui screenshot -a $AppPid -o 'screenshots/05-settings.png' 2>$null

if (-not [string]::IsNullOrWhiteSpace($MediaPath))
{
    Test-UI 'Navigate to Edition for trim controls' { winapp ui invoke 'EditionNavigationItem' -a $AppPid }
    Test-UI 'Load media for trim controls' {
        if (-not (Test-Path -LiteralPath $MediaPath -PathType Leaf))
        {
            throw "Media file not found: $MediaPath"
        }

        winapp ui invoke 'OpenMediaEmptyStateButton' -a $AppPid
        Start-Sleep -Milliseconds 500
        $windows = winapp ui list-windows -a $AppPid --json 2>$null | ConvertFrom-Json
        $picker = $windows | Where-Object { $_.title -match 'Open|Ouvrir' } | Select-Object -First 1
        if (-not $picker)
        {
            throw 'The media file picker did not open.'
        }

        winapp ui set-value 'FileNameControlHost' (Resolve-Path -LiteralPath $MediaPath).Path -w $picker.hwnd
        winapp ui send-keys 'enter' -w $picker.hwnd --via send-input
    }
    Test-UI 'Set start control exists' { winapp ui wait-for 'SetStartAtPlaybackButton' -a $AppPid -t 5000 }
    Test-UI 'Set end control exists' { winapp ui wait-for 'SetEndAtPlaybackButton' -a $AppPid -t 5000 }
    Test-UI 'Set start shortcut is handled' {
        winapp ui send-keys 'i' -a $AppPid --via send-input
        winapp ui wait-for 'SetStartAtPlaybackButton' -a $AppPid -t 2000
    }
    Test-UI 'Set end shortcut is handled' {
        winapp ui send-keys 'o' -a $AppPid --via send-input
        winapp ui wait-for 'SetEndAtPlaybackButton' -a $AppPid -t 2000
    }
    winapp ui screenshot -a $AppPid -o 'screenshots/06-trim-controls.png' 2>$null
}

$elements = (winapp ui inspect -a $AppPid --interactive --json 2>$null | ConvertFrom-Json).windows.elements
$missingAutomationIds = @($elements | Where-Object {
    $_.type -match 'Button|TextBox|ComboBox|CheckBox|ToggleSwitch|ListItem' -and
    $_.name -notmatch 'Minimize|Maximize|Close|System|Réduire|Agrandir|Fermer|Autres options' -and
    -not $_.automationId
})

if ($missingAutomationIds.Count -eq 0)
{
    $pass++
    $results += @{ name = 'Interactive controls expose automation IDs'; status = 'PASS' }
}
else
{
    $fail++
    $details = ($missingAutomationIds | ForEach-Object { "$($_.type) '$($_.name)'" }) -join ', '
    $results += @{ name = 'Interactive controls expose automation IDs'; status = 'FAIL'; detail = "Missing: $details" }
}

Write-Host "Passed: $pass | Failed: $fail"
$results | Where-Object { $_.status -eq 'FAIL' } | ForEach-Object {
    Write-Host "  FAIL: $($_.name) - $($_.detail)" -ForegroundColor Red
}
$results | ConvertTo-Json | Out-File 'test-results.json'
if ($fail -gt 0) { exit 1 }
