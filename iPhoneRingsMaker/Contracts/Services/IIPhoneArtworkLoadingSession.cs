namespace iPhoneRingsMaker.Contracts.Services;

public interface IIPhoneArtworkLoadingSession : IAsyncDisposable
{
    Task Completion
    {
        get;
    }

    void SetVisibleTracks(IReadOnlyCollection<string> trackIdentifiers);
}
