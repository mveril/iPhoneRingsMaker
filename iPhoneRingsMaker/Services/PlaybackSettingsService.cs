using iPhoneRingsMaker.Contracts.Services;

namespace iPhoneRingsMaker.Services;

public class PlaybackSettingsService(ILocalSettingsService localSettingsService) : IPlaybackSettingsService
{
    private const int DefaultSkipIntervalSeconds = 5;
    private const int MinimumSkipIntervalSeconds = 1;
    private const int MaximumSkipIntervalSeconds = 10;
    private const string SkipIntervalSettingsKey = "PlaybackSkipIntervalSeconds";

    public event EventHandler? SkipIntervalSecondsChanged;

    public int SkipIntervalSeconds
    {
        get;
        private set;
    } = DefaultSkipIntervalSeconds;

    public async Task InitializeAsync()
    {
        var savedValue = await localSettingsService.ReadSettingAsync<int?>(SkipIntervalSettingsKey);
        SkipIntervalSeconds = Normalize(savedValue ?? DefaultSkipIntervalSeconds);
    }

    public async Task SetSkipIntervalSecondsAsync(int value)
    {
        var normalizedValue = Normalize(value);
        if (SkipIntervalSeconds == normalizedValue)
        {
            return;
        }

        SkipIntervalSeconds = normalizedValue;
        await localSettingsService.SaveSettingAsync(SkipIntervalSettingsKey, SkipIntervalSeconds);
        SkipIntervalSecondsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static int Normalize(int value) =>
        Math.Clamp(value, MinimumSkipIntervalSeconds, MaximumSkipIntervalSeconds);
}
