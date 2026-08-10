using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Helpers;

using Microsoft.UI.Xaml;

using Windows.ApplicationModel;

namespace iPhoneRingsMaker.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly IPlaybackSettingsService _playbackSettingsService;
    private readonly IThemeSelectorService _themeSelectorService;

    [ObservableProperty]
    public partial ElementTheme ElementTheme
    {
        get; set;
    }

    [ObservableProperty]
    public partial string VersionDescription
    {
        get; set;
    }

    [ObservableProperty]
    public partial double SkipIntervalSeconds
    {
        get; set;
    }

    public SettingsViewModel(
        IThemeSelectorService themeSelectorService,
        IPlaybackSettingsService playbackSettingsService)
    {
        _themeSelectorService = themeSelectorService;
        _playbackSettingsService = playbackSettingsService;
        ElementTheme = _themeSelectorService.Theme;
        SkipIntervalSeconds = _playbackSettingsService.SkipIntervalSeconds;
        VersionDescription = GetVersionDescription();

    }

    public async Task SetSkipIntervalSecondsAsync(double value)
    {
        var interval = (int)Math.Round(value);
        await _playbackSettingsService.SetSkipIntervalSecondsAsync(interval);
        SkipIntervalSeconds = _playbackSettingsService.SkipIntervalSeconds;
    }

    [RelayCommand]
    private async Task SwitchThemeAsync(ElementTheme theme)
    {
        if (ElementTheme != theme)
        {
            ElementTheme = theme;
            await _themeSelectorService.SetThemeAsync(theme);
        }
    }

    private static string GetVersionDescription()
    {
        Version version;

        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;

            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
        }

        return $"{"AppDisplayName".GetLocalized()} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
