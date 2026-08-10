namespace iPhoneRingsMaker.Contracts.Services;

public interface IPlaybackSettingsService
{
    int SkipIntervalSeconds
    {
        get;
    }

    Task InitializeAsync();

    Task SetSkipIntervalSecondsAsync(int value);
}
