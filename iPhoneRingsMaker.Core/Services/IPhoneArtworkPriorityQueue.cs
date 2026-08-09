using iPhoneRingsMaker.Core.Models;

namespace iPhoneRingsMaker.Core.Services;

internal sealed class IPhoneArtworkPriorityQueue(IEnumerable<IPhoneMusicTrack> tracks)
{
    private readonly List<IPhoneMusicTrack> _pendingTracks = tracks.ToList();
    private readonly object _stateLock = new();
    private HashSet<string> _visibleTrackIdentifiers = new(StringComparer.Ordinal);

    public void SetVisibleTracks(IEnumerable<string> trackIdentifiers)
    {
        ArgumentNullException.ThrowIfNull(trackIdentifiers);
        lock (_stateLock)
        {
            _visibleTrackIdentifiers = new HashSet<string>(trackIdentifiers, StringComparer.Ordinal);
        }
    }

    public bool TryDequeue(out IPhoneMusicTrack? track)
    {
        lock (_stateLock)
        {
            if (_pendingTracks.Count == 0)
            {
                track = null;
                return false;
            }

            var index = _pendingTracks.FindIndex(candidate =>
                _visibleTrackIdentifiers.Contains(candidate.PersistentIdentifier));
            if (index < 0)
            {
                index = 0;
            }

            track = _pendingTracks[index];
            _pendingTracks.RemoveAt(index);
            return true;
        }
    }
}
