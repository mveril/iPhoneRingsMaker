using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace iPhoneRingsMaker.Views;

public sealed partial class EditionPage : Page
{
    private readonly MediaPlayer _player;
    private readonly IPlaybackSettingsService _playbackSettingsService;
    private bool _hasShownTrimTip;

    public EditionViewModel ViewModel
    {
        get;
    }

    public EditionPage(
        EditionViewModel viewModel,
        IPlaybackSettingsService playbackSettingsService)
    {
        ViewModel = viewModel;
        _playbackSettingsService = playbackSettingsService;
        InitializeComponent();
        _player = new MediaPlayer();
        MediaElement.SetMediaPlayer(_player);
        _player.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        Loaded += EditionPage_Loaded;
        Unloaded += EditionPage_Unloaded;
    }

    private void EditionPage_Loaded(object sender, RoutedEventArgs e)
    {
        PlaybackTransportControls.SkipIntervalSeconds = _playbackSettingsService.SkipIntervalSeconds;
        _playbackSettingsService.SkipIntervalSecondsChanged += PlaybackSettingsService_SkipIntervalSecondsChanged;
        ViewModel.IsActive = true;
    }

    private void EditionPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _playbackSettingsService.SkipIntervalSecondsChanged -= PlaybackSettingsService_SkipIntervalSecondsChanged;
        ViewModel.IsActive = false;
    }

    private void PlaybackSettingsService_SkipIntervalSecondsChanged(object? sender, EventArgs e)
    {
        PlaybackTransportControls.SkipIntervalSeconds = _playbackSettingsService.SkipIntervalSeconds;
    }

    private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
    {
        RefreshPosition(sender);
    }

    private void RefreshPosition(MediaPlaybackSession session)
    {
        if (session.Position < ViewModel.StartTime)
        {
            session.Position = ViewModel.StartTime;
        }

        if (session.Position >= ViewModel.EndTime)
        {
            session.MediaPlayer.Pause();
            session.Position = ViewModel.StartTime;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.Source))
        {
            _player.Source = ViewModel.Source;
            if (ViewModel.Source is not null && !_hasShownTrimTip)
            {
                _hasShownTrimTip = true;
                TrimTeachingTip.IsOpen = true;
            }
        }
        if (e.PropertyName is nameof(ViewModel.StartTime) or nameof(ViewModel.EndTime))
        {
            RefreshPosition(_player.PlaybackSession);
        }
    }

    private async void MediaPlayerElement_Drop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        var files = await e.DataView.GetStorageItemsAsync();
        if (files.FirstOrDefault() is IStorageFile file)
        {
            await ViewModel.InitializeAsync(file);
        }
    }

    private void MediaPlayerElement_DragEnter(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
    }

    private void PlaybackTransportControls_SkipRequested(object? sender, int seconds)
    {
        var position = _player.PlaybackSession.Position + TimeSpan.FromSeconds(seconds);
        _player.PlaybackSession.Position = position < ViewModel.StartTime
            ? ViewModel.StartTime
            : position > ViewModel.EndTime
                ? ViewModel.EndTime
                : position;
    }

}
