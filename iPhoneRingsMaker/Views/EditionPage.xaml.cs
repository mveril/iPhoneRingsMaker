using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace iPhoneRingsMaker.Views;

public sealed partial class EditionPage : Page
{
    private readonly MediaPlayer _player;
    private bool _hasShownTrimTip;

    public EditionViewModel ViewModel
    {
        get;
    }

    public EditionPage()
    {
        ViewModel = App.GetService<EditionViewModel>();
        InitializeComponent();
        _player = new MediaPlayer();
        MediaElement.SetMediaPlayer(_player);
        _player.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

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

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.IsActive = true;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.IsActive = false;
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

}
