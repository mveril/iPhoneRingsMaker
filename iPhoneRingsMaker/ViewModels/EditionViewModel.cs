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
    private readonly IMediaFactory _mediaFactory;
    private readonly IM4RProjectFactory _projectFactory;
    private IMedia? _currentMedia;

    public EditionViewModel(
        IM4RProjectManager projectManager,
        INavigationService navigationService,
        IFolderLauncherService folderLauncherService,
        IMediaFactory mediaFactory,
        IM4RProjectFactory projectFactory)
    {
        _projectManager = projectManager;
        _navigationService = navigationService;
        _folderLauncherService = folderLauncherService;
        _mediaFactory = mediaFactory;
        _projectFactory = projectFactory;
        _projectManager.ProjectLoaded += ProjectManager_ProjectLoaded;
        _projectManager.ProjectUnloaded += ProjectManager_ProjectUnloaded;
    }

    private void ProjectManager_ProjectUnloaded(object? sender, ProjectEventArgs e)
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

    private async void ProjectManager_ProjectLoaded(object? sender, ProjectEventArgs e)
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
    [NotifyCanExecuteChangedFor(nameof(ContinueToConversionCommand))]
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
            if (_projectManager.IsProjectOpen)
            {
                return _projectManager.Project.StartTime;
            }
            else
            {
                return TimeSpan.Zero;
            }
        }
        set
        {
            if (!_projectManager.IsProjectOpen || MaximumDuration is null)
            {
                return;
            }

            var clampedValue = value < TimeSpan.Zero
                ? TimeSpan.Zero
                : value > MaximumDuration.Value
                    ? MaximumDuration.Value
                    : value;

            if (clampedValue >= MaximumDuration.Value)
            {
                return;
            }

            if (_projectManager.Project.StartTime != clampedValue)
            {
                _projectManager.Project.StartTime = clampedValue;
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
            if (!_projectManager.IsProjectOpen || MaximumDuration is null)
            {
                return TimeSpan.Zero;
            }
            if (!_projectManager.Project.EndTime.HasValue)
            {
                return MaximumDuration.Value;
            }
            return _projectManager.Project.EndTime.Value;
        }
        set
        {
            if (!_projectManager.IsProjectOpen || !MaximumDuration.HasValue)
            {
                return;
            }
            var maximumEndTime = StartTime + RingtoneConstraints.MaximumDuration;
            if (maximumEndTime > MaximumDuration.Value)
            {
                maximumEndTime = MaximumDuration.Value;
            }

            if (value <= StartTime)
            {
                return;
            }

            var clampedValue = value > maximumEndTime
                ? maximumEndTime
                : value;

            _projectManager.Project.EndTime = clampedValue >= MaximumDuration.Value
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
    private readonly IM4RProjectManager _projectManager;

    public async Task InitializeAsync()
    {
        if (!_projectManager.IsProjectOpen)
        {
            return;
        }
        var project = _projectManager.Project;
        if (project is null)
        {
            return;
        }
        await LoadMediaSource(project.MediaSource);
        OnPropertyChanged(nameof(StartTime));
    }

    private void Project_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not null && sender is M4RProject)
        {
            switch (e.PropertyName)
            {
                case nameof(M4RProject.StartTime):
                    OnPropertyChanged(nameof(StartTime));
                    OnPropertyChanged(nameof(Duration));
                    OnPropertyChanged(nameof(StartTimeDisplay));
                    OnPropertyChanged(nameof(DurationDisplay));
                    ContinueToConversionCommand.NotifyCanExecuteChanged();
                    break;
                case nameof(M4RProject.EndTime):
                    OnPropertyChanged(nameof(EndTime));
                    OnPropertyChanged(nameof(Duration));
                    OnPropertyChanged(nameof(EndTimeDisplay));
                    OnPropertyChanged(nameof(DurationDisplay));
                    ContinueToConversionCommand.NotifyCanExecuteChanged();
                    break;
            }
        }
    }

    private async Task LoadMediaSource(IProjectMediaSource mediaSource)
    {
        await LoadMedia(_mediaFactory.Create(mediaSource));
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

    internal Task InitializeAsync(IStorageFile storageFile)
    {
        var mediaSource = new LocalMediaSource() { Path = storageFile.Path };
        _projectManager.Project = _projectFactory.Create(mediaSource);
        return Task.CompletedTask;
    }

    public async Task OpenMediaSourceAsync(IProjectMediaSource mediaSource)
    {
        ArgumentNullException.ThrowIfNull(mediaSource);
        _isReplacingProject = true;
        try
        {
            _projectManager.Project = _projectFactory.Create(mediaSource);
        }
        finally
        {
            _isReplacingProject = false;
        }

        await InitializeAsync();
    }

    internal void SetStartAtPlayback(TimeSpan position)
    {
        if (position > StartTime && position < EndTime)
        {
            StartTime = position;
        }
    }

    internal void SetEndAtPlayback(TimeSpan position)
    {
        if (position > StartTime && position < EndTime)
        {
            EndTime = position;
        }
    }

    private bool CanContinueToConversion() =>
        MaximumDuration.HasValue
        && RingtoneConstraints.IsValidRange(StartTime, EndTime, MaximumDuration.Value);

    [RelayCommand(CanExecute = nameof(CanContinueToConversion))]
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
        _projectManager.Project?.MediaSource is LocalMediaSource localMediaSource
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
