using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.UI.Controls;
using iPhoneRingsMaker.Contracts.Models;
using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Contracts.ViewModels;
using iPhoneRingsMaker.Core.Contracts.Services;
using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Helpers;
using iPhoneRingsMaker.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Audio;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using IProjectMediaSource = iPhoneRingsMaker.Core.Models.IMediaSource;

namespace iPhoneRingsMaker.ViewModels;

public partial class EditionViewModel : ObservableRecipient, INavigationAware
{
    private bool _isReplacingProject;
    private readonly INavigationService _navigationService;
    private readonly IFolderLauncherService _folderLauncherService;
    private IMedia? _currentMedia;

    public EditionViewModel(
        IM4RProjManager M4RProjManager,
        INavigationService navigationService,
        IFolderLauncherService folderLauncherService)
    {
        this.M4RProjManager = M4RProjManager;
        _navigationService = navigationService;
        _folderLauncherService = folderLauncherService;
        this.M4RProjManager.ProjectLoaded += M4RProjManager_ProjectLoaded;
        this.M4RProjManager.ProjectUnloaded += M4RProjManager_ProjectUnloaded;
    }

    private void M4RProjManager_ProjectUnloaded(object? sender, ProjectEventArgs e)
    {
        e.Project.PropertyChanged -= Project_PropertyChanged;
        UnInitialize();
    }
    private void UnInitialize()
    {
        Source = null;
        Artwork = null;
        Metadata = null;
        _currentMedia = null;
        CopyArtworkCommand.NotifyCanExecuteChanged();
        ShowMediaInExplorerCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(StartTime));
        OnPropertyChanged(nameof(EndTime));
    }

    private async void M4RProjManager_ProjectLoaded(object? sender, ProjectEventArgs e)
    {
        e.Project.PropertyChanged += Project_PropertyChanged;
        if (!_isReplacingProject)
        {
            await InitializeAsync();
        }
    }

    public TimeSpan Duration
    {
        get => EndTime - StartTime;
        set
        {
            if (Source is not null)
            {
                EndTime = StartTime + value;
            }
        }
    }

    public string StartTimeDisplay => FormatTime(StartTime);

    public string EndTimeDisplay => FormatTime(EndTime);

    public string DurationDisplay => FormatTime(Duration);

    public TimeSpan? MaximumDuration => Metadata?.Duration;

    public string? Title => Metadata?.Title;

    public string? Album => Metadata?.Album;

    public string? Artist => Metadata?.Artist;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title), nameof(Album), nameof(Artist), nameof(MaximumDuration), nameof(EndTime))]
    public partial MusicMetadata? Metadata
    {
        get; set;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMedia), nameof(HasNoMedia), nameof(ShowEmptyState))]
    public partial MediaPlaybackItem? Source
    {
        get; set;
    }

    public bool HasMedia => Source is not null;

    public bool HasNoMedia => !HasMedia;

    public bool ShowEmptyState => HasNoMedia && !IsLoading;

    public bool CanCopyArtwork => Artwork is not null && _currentMedia is not null;

    public bool CanShowMediaInExplorer => GetLocalMediaPath() is { } path && File.Exists(path);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage
    {
        get; set;
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    public partial bool IsLoading
    {
        get; set;
    }

    public TimeSpan StartTime
    {
        get
        {
            if (M4RProjManager.IsProjectOpen)
            {
                return M4RProjManager.Project.StartTime;
            }
            else
            {
                return TimeSpan.Zero;
            }
        }
        set
        {
            if (!M4RProjManager.IsProjectOpen || MaximumDuration is null)
            {
                return;
            }

            var clampedValue = value < TimeSpan.Zero
                ? TimeSpan.Zero
                : value > MaximumDuration.Value
                    ? MaximumDuration.Value
                    : value;

            if (M4RProjManager.Project.StartTime != clampedValue)
            {
                M4RProjManager.Project.StartTime = clampedValue;
                if (EndTime <= clampedValue || EndTime - clampedValue > RingtoneConstraints.MaximumDuration)
                {
                    EndTime = clampedValue + RingtoneConstraints.MaximumDuration;
                }
            }
        }
    }

    public TimeSpan EndTime
    {
        get
        {
            if (!M4RProjManager.IsProjectOpen || MaximumDuration is null)
            {
                return TimeSpan.Zero;
            }
            if (!M4RProjManager.Project.EndTime.HasValue)
            {
                return MaximumDuration.Value;
            }
            return M4RProjManager.Project.EndTime.Value;
        }
        set
        {
            if (!M4RProjManager.IsProjectOpen || !MaximumDuration.HasValue)
            {
                return;
            }
            var maximumEndTime = StartTime + RingtoneConstraints.MaximumDuration;
            if (maximumEndTime > MaximumDuration.Value)
            {
                maximumEndTime = MaximumDuration.Value;
            }

            var clampedValue = value <= StartTime
                ? StartTime
                : value > maximumEndTime
                    ? maximumEndTime
                    : value;

            M4RProjManager.Project.EndTime = clampedValue >= MaximumDuration.Value
                ? null
                : clampedValue;
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyArtworkCommand))]
    public partial ImageSource? Artwork
    {
        get; set;
    }
    private readonly IM4RProjManager M4RProjManager;

    public async Task InitializeAsync()
    {
        if (!M4RProjManager.IsProjectOpen)
        {
            return;
        }
        var project = M4RProjManager.Project;
        if (project is null)
        {
            return;
        }
        await LoadMediaSource(project.MediaSource);
        OnPropertyChanged(nameof(StartTime));
    }

    private void Project_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not null && sender is M4RProj)
        {
            switch (e.PropertyName)
            {
                case nameof(M4RProj.StartTime):
                    OnPropertyChanged(nameof(StartTime));
                    OnPropertyChanged(nameof(Duration));
                    OnPropertyChanged(nameof(StartTimeDisplay));
                    OnPropertyChanged(nameof(DurationDisplay));
                    break;
                case nameof(M4RProj.EndTime):
                    OnPropertyChanged(nameof(EndTime));
                    OnPropertyChanged(nameof(Duration));
                    OnPropertyChanged(nameof(EndTimeDisplay));
                    OnPropertyChanged(nameof(DurationDisplay));
                    break;
            }
        }
    }

    private async Task LoadMediaSource(IProjectMediaSource mediaSource)
    {
        await LoadMedia(mediaSource.GetMedia());
    }

    private static string FormatTime(TimeSpan value) => value.ToString(@"mm\:ss\.f");

    private async Task LoadMedia(IMedia media)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            _currentMedia = media;
            Artwork = null;
            Metadata = null;
            Source = await media.GetMediaPlaybackItemAsync();
            Artwork = await media.GetArtworkAsync(400);
            Metadata = await media.GetMusicMetadataAsync();
            ShowMediaInExplorerCommand.NotifyCanExecuteChanged();
        }
        catch (Exception exception)
        {
            exception.RethrowIfDebuggerAttached();
            Source = null;
            ErrorMessage = string.Format(
                ResourceExtensions.GetLocalized("Edition_LoadError"),
                exception.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    internal async Task InitializeAsync(IStorageFile storageFile)
    {
        var mediaSource = new LocalMediaSource() { Path = storageFile.Path };
        var proj = new M4RProj() { MediaSource = mediaSource };
        M4RProjManager.Project = proj;
    }

    public async Task OpenMediaSourceAsync(IProjectMediaSource mediaSource)
    {
        ArgumentNullException.ThrowIfNull(mediaSource);
        _isReplacingProject = true;
        try
        {
            M4RProjManager.Project = new M4RProj { MediaSource = mediaSource };
        }
        finally
        {
            _isReplacingProject = false;
        }

        await InitializeAsync();
    }

    [RelayCommand]
    private void ContinueToConversion() =>
        _navigationService.NavigateTo(typeof(ConversionViewModel).FullName!);

    [RelayCommand(CanExecute = nameof(CanCopyArtwork))]
    private async Task CopyArtworkAsync()
    {
        var artwork = _currentMedia is null
            ? null
            : await _currentMedia.GetArtworkStreamAsync(1200);
        if (artwork is null)
        {
            return;
        }

        var package = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
        package.SetBitmap(artwork);
        Clipboard.SetContent(package);
        Clipboard.Flush();
    }

    [RelayCommand(CanExecute = nameof(CanShowMediaInExplorer))]
    private async Task ShowMediaInExplorerAsync()
    {
        if (GetLocalMediaPath() is { } path)
        {
            await _folderLauncherService.ShowFileAsync(path);
        }
    }

    private string? GetLocalMediaPath() =>
        M4RProjManager.Project?.MediaSource is LocalMediaSource localMediaSource
            ? localMediaSource.Path
            : null;

    public async void OnNavigatedTo(object parameter)
    {
        await InitializeAsync();
    }
    public void OnNavigatedFrom()
    {
        UnInitialize();
    }
}
