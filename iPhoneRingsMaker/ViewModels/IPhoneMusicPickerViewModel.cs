using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Helpers;
using iPhoneRingsMaker.Models;

namespace iPhoneRingsMaker.ViewModels;

public partial class IPhoneMusicPickerViewModel : ObservableObject, IDisposable
{
    private readonly IAppleDeviceService _deviceService;
    private readonly IAppleMusicLibraryService _musicLibraryService;
    private readonly Dictionary<string, IPhoneMusicTrackItem> _trackItems = new(StringComparer.Ordinal);
    private readonly HashSet<string> _visibleTrackIdentifiers = new(StringComparer.Ordinal);
    private IReadOnlyList<IPhoneMusicTrackItem> _allTracks = [];
    private CancellationTokenSource? _artworkCancellation;
    private IIPhoneArtworkLoadingSession? _artworkSession;
    private CancellationTokenSource? _loadCancellation;
    private bool _isActive;
    private bool _hasCompletedDeviceDiscovery;

    public IPhoneMusicPickerViewModel(
        IAppleDeviceService deviceService,
        IAppleMusicLibraryService musicLibraryService)
    {
        _deviceService = deviceService;
        _musicLibraryService = musicLibraryService;
    }

    public ObservableCollection<AppleDeviceInfo> Devices { get; } = [];

    [ObservableProperty]
    public partial AppleDeviceInfo? SelectedDevice
    {
        get; set;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUseSelection))]
    public partial IPhoneMusicTrackItem? SelectedTrack
    {
        get; set;
    }

    [ObservableProperty]
    public partial IReadOnlyList<IPhoneMusicTrackItem> FilteredTracks { get; set; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUseSelection), nameof(IsSearchEnabled))]
    [NotifyPropertyChangedFor(nameof(HasNoDevices))]
    public partial bool IsLoading
    {
        get; set;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoDevices))]
    public partial bool IsStatusOpen
    {
        get; set;
    }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial PickerStatusSeverity StatusSeverity
    {
        get; set;
    }

    public bool CanUseSelection => !IsLoading && SelectedTrack is { IsAvailable: true };

    public bool IsSearchEnabled => !IsLoading;

    public bool HasNoDevices =>
        _hasCompletedDeviceDiscovery
        && !IsLoading
        && !IsStatusOpen
        && Devices.Count == 0;

    public async Task ActivateAsync()
    {
        if (_isActive)
        {
            return;
        }

        _isActive = true;
        _deviceService.DevicesChanged += OnDevicesChanged;
        _deviceService.StartWatching();
        await RefreshDevicesAsync();
    }

    public void Deactivate()
    {
        if (!_isActive)
        {
            return;
        }

        _isActive = false;
        _loadCancellation?.Cancel();
        _loadCancellation = null;
        StopArtworkLoading();
        _deviceService.DevicesChanged -= OnDevicesChanged;
        _deviceService.StopWatching();
    }

    public IPhoneMediaSource? CreateSelectedSource()
    {
        if (SelectedDevice is not { } device
            || SelectedTrack is not { IsAvailable: true } item)
        {
            return null;
        }

        return new IPhoneMediaSource
        {
            DeviceIdentifier = device.Identifier,
            DeviceDisplayName = device.Name,
            TrackPersistentIdentifier = item.Track.PersistentIdentifier,
            TrackDisplayName = item.Track.DisplayName,
            TrackTitle = item.Track.Title,
            RemotePathHint = item.Track.RemotePath,
        };
    }

    public void Dispose()
    {
        Deactivate();
        GC.SuppressFinalize(this);
    }

    public void SetTrackVisibility(IPhoneMusicTrackItem item, bool isVisible)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (isVisible)
        {
            _visibleTrackIdentifiers.Add(item.Track.PersistentIdentifier);
        }
        else
        {
            _visibleTrackIdentifiers.Remove(item.Track.PersistentIdentifier);
        }

        try
        {
            _artworkSession?.SetVisibleTracks(_visibleTrackIdentifiers);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    partial void OnSelectedDeviceChanged(AppleDeviceInfo? value)
    {
        if (value is null)
        {
            _loadCancellation?.Cancel();
            StopArtworkLoading();
            _allTracks = [];
            _trackItems.Clear();
            _visibleTrackIdentifiers.Clear();
            FilteredTracks = [];
            return;
        }

        LoadTracksCommand.Execute(value);
    }

    partial void OnSelectedTrackChanged(IPhoneMusicTrackItem? value)
    {
        if (value is { IsAvailable: false })
        {
            ShowStatus(value.AvailabilityDescription, PickerStatusSeverity.Warning);
            SelectedTrack = null;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private async void OnDevicesChanged(object? sender, EventArgs e) => await RefreshDevicesAsync();

    private async Task RefreshDevicesAsync()
    {
        try
        {
            var selectedIdentifier = SelectedDevice?.Identifier;
            var previousIdentifiers = Devices
                .Select(device => device.Identifier)
                .ToHashSet(StringComparer.Ordinal);
            var devices = await _deviceService.GetDevicesAsync();
            var currentIdentifiers = devices
                .Select(device => device.Identifier)
                .ToHashSet(StringComparer.Ordinal);
            foreach (var disconnectedIdentifier in previousIdentifiers.Except(currentIdentifiers))
            {
                _musicLibraryService.InvalidateCatalog(disconnectedIdentifier);
            }

            Devices.Clear();
            foreach (var device in devices)
            {
                Devices.Add(device);
            }

            OnPropertyChanged(nameof(HasNoDevices));

            SelectedDevice = Devices.FirstOrDefault(device => device.Identifier == selectedIdentifier)
                ?? Devices.FirstOrDefault();

            if (Devices.Count == 0)
            {
                IsStatusOpen = false;
            }
        }
        catch (Exception exception)
        {
            exception.RethrowIfDebuggerAttached();
            ShowStatus(exception.Message, PickerStatusSeverity.Error);
        }
        finally
        {
            _hasCompletedDeviceDiscovery = true;
            OnPropertyChanged(nameof(HasNoDevices));
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task LoadTracksAsync(AppleDeviceInfo device)
    {
        _loadCancellation?.Cancel();
        StopArtworkLoading();
        var loadCancellation = new CancellationTokenSource();
        _loadCancellation = loadCancellation;
        var cancellationToken = loadCancellation.Token;

        try
        {
            IsLoading = true;
            IsStatusOpen = false;
            SelectedTrack = null;
            FilteredTracks = [];

            if (!device.IsPaired)
            {
                device = await _deviceService.PairAsync(device, cancellationToken: cancellationToken);
            }

            var tracks = await _musicLibraryService.GetTracksAsync(device, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _allTracks = tracks.Select(track => new IPhoneMusicTrackItem(track)).ToArray();
            _trackItems.Clear();
            foreach (var item in _allTracks)
            {
                _trackItems[item.Track.PersistentIdentifier] = item;
            }

            _visibleTrackIdentifiers.Clear();
            ApplyFilter();

            if (_allTracks.Count == 0)
            {
                ShowStatus("MusicLibrary_Empty".GetLocalized(), PickerStatusSeverity.Informational);
            }
            else
            {
                _ = LoadArtworkAsync(device, tracks);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            exception.RethrowIfDebuggerAttached();
            ShowStatus(exception.Message, PickerStatusSeverity.Error);
        }
        finally
        {
            loadCancellation.Dispose();
            if (ReferenceEquals(_loadCancellation, loadCancellation))
            {
                _loadCancellation = null;
                IsLoading = false;
            }
        }
    }

    private async Task LoadArtworkAsync(
        AppleDeviceInfo device,
        IReadOnlyList<IPhoneMusicTrack> tracks)
    {
        var artworkCancellation = new CancellationTokenSource();
        _artworkCancellation = artworkCancellation;
        var progress = new Progress<IPhoneMusicTrack>(track =>
        {
            if (_trackItems.TryGetValue(track.PersistentIdentifier, out var item))
            {
                item.UpdateTrack(track);
            }
        });

        try
        {
            await using var session = await _musicLibraryService.CreateArtworkLoadingSessionAsync(
                device,
                tracks,
                progress,
                artworkCancellation.Token);
            if (!ReferenceEquals(_artworkCancellation, artworkCancellation))
            {
                return;
            }

            _artworkSession = session;
            session.SetVisibleTracks(_visibleTrackIdentifiers);
            await session.Completion;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            exception.RethrowIfDebuggerAttached();
        }
        finally
        {
            if (ReferenceEquals(_artworkCancellation, artworkCancellation))
            {
                _artworkCancellation = null;
                _artworkSession = null;
            }

            artworkCancellation.Dispose();
        }
    }

    private void StopArtworkLoading()
    {
        _artworkCancellation?.Cancel();
        _artworkSession = null;
        _visibleTrackIdentifiers.Clear();
    }

    private void ApplyFilter()
    {
        var query = SearchText.Trim();
        FilteredTracks = string.IsNullOrWhiteSpace(query)
            ? _allTracks
            : _allTracks.Where(item =>
                item.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || item.Artist.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || item.Album.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                .ToArray();
    }

    private void ShowStatus(string message, PickerStatusSeverity severity)
    {
        StatusMessage = message;
        StatusSeverity = severity;
        IsStatusOpen = true;
    }
}
