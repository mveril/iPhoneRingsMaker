namespace iPhoneRingsMaker.Contracts.Services;

public interface IPlaybackSettingsService
{
    event EventHandler? SkipIntervalSecondsChanged;

    int SkipIntervalSeconds
    {
        get;
    }

    Task InitializeAsync();

    Task SetSkipIntervalSecondsAsync(int value);
}
