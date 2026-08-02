using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iPhoneRingsMaker.Contracts.Models;
using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Contracts.ViewModels;
using iPhoneRingsMaker.Core.Contracts.Services;
using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Helpers;
using iPhoneRingsMaker.Services;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Playback;
using IProjectMediaSource = iPhoneRingsMaker.Core.Models.IMediaSource;

namespace iPhoneRingsMaker.ViewModels;

public partial class ConversionViewModel : ObservableObject, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly IFilePickerService _filePickerService;
    private readonly IFolderLauncherService _folderLauncherService;
    private readonly IMediaFileNameService _mediaFileNameService;
    private readonly IMediaFactory _mediaFactory;
    private readonly IRingtoneConversionService _conversionService;

    public ConversionViewModel(
        IM4RProjectManager projectManager,
        INavigationService navigationService,
        IFilePickerService filePickerService,
        IFolderLauncherService folderLauncherService,
        IMediaFileNameService mediaFileNameService,
        IMediaFactory mediaFactory,
        IRingtoneConversionService conversionService)
    {
        _projectManager = projectManager;
        _navigationService = navigationService;
        _filePickerService = filePickerService;
        _folderLauncherService = folderLauncherService;
        _mediaFileNameService = mediaFileNameService;
        _mediaFactory = mediaFactory;
        _conversionService = conversionService;
        _projectManager.ProjectLoaded += ProjectManager_ProjectLoaded;
        _projectManager.ProjectUnloaded += ProjectManager_ProjectUnloaded;
    }

    private void ProjectManager_ProjectUnloaded(object? sender, ProjectEventArgs e)
    {
        e.Project.PropertyChanged -= Project_PropertyChanged;
        UnInitializeAsync();
    }
    private void UnInitializeAsync()
    {
        Source = null;
        OnPropertyChanged(nameof(Source));
    }

    private async void ProjectManager_ProjectLoaded(object? sender, ProjectEventArgs e)
    {
        e.Project.PropertyChanged += Project_PropertyChanged;
        await InitializeAsync();
    }

    private async void Project_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        await ReloadProject((sender as M4RProject)!);
    }

    private async Task InitializeAsync()
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
        await ReloadProject(project);
    }

    private async Task ReloadProject(M4RProject project)
    {
        _media = _mediaFactory.Create(project.MediaSource);
        MediaPlaybackItem source;
        if (project.EndTime.HasValue)
        {
            source = await _media.GetMediaPlaybackItemAsync(project.StartTime, project.EndTime.Value - project.StartTime);
        }
        else if (project.StartTime != TimeSpan.Zero)
        {
            source = await _media.GetMediaPlaybackItemAsync(project.StartTime);
        }
        else
        {
            source = await _media.GetMediaPlaybackItemAsync();
        }
        await source.Source.OpenAsync();
        Source = source;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressDescription))]
    public partial int TranscodeProgress
    {
        get; set;
    }

    public string ProgressDescription => string.Format(
        "Conversion_ProgressFormat".GetLocalized(),
        TranscodeProgress);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConvertCommand))]
    [NotifyPropertyChangedFor(nameof(CanPrepareTransfer))]
    [NotifyPropertyChangedFor(nameof(HasSource), nameof(HasNoSource))]
    public partial MediaPlaybackItem? Source
    {
        get; set;
    }

    public bool HasSource => Source is not null;

    public bool HasNoSource => !HasSource;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConvertCommand))]
    [NotifyPropertyChangedFor(nameof(CanTransfer))]
    public partial string? OutputPath
    {
        get; set;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanTransfer))]
    [NotifyPropertyChangedFor(nameof(CanPrepareTransfer))]
    public partial bool IsTranscoding
    {
        get; set;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanTransfer))]
    public partial bool HasConvertedOutput
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsTranscodingStarted
    {
        get; set;
    }

    [ObservableProperty]
    public partial string? ErrorMessage
    {
        get; set;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSuccess))]
    public partial string? SuccessMessage
    {
        get; set;
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    public bool CanTransfer =>
        HasConvertedOutput
        && !IsTranscoding
        && !string.IsNullOrWhiteSpace(OutputPath)
        && File.Exists(OutputPath);

    public bool CanPrepareTransfer => !IsTranscoding && HasValidRingtoneSelection();

    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnIsTranscodingChanged(bool value)
    {
        if (IsTranscoding)
        {
            IsTranscodingStarted = true;
        }
    }

    partial void OnOutputPathChanged(string? value)
    {
        HasConvertedOutput = false;
    }

    private IMedia? _media;
    private readonly IM4RProjectManager _projectManager;

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task OnSelectSaveFile()
    {
        var path = await _filePickerService.PickSaveFileAsync(
            "iOS ringtone file",
            ".m4r",
            await GetSuggestedRingtoneNameAsync());
        if (path is not null)
        {
            OutputPath = path;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanConvert), IncludeCancelCommand = true)]
    private async Task OnConvertAsync(CancellationToken cancellationToken)
    {
        try
        {
            ErrorMessage = null;
            SuccessMessage = null;
            TranscodeProgress = 0;
            IsTranscoding = true;
            var outputPath = OutputPath ?? throw new InvalidOperationException("No output file is selected.");
            await ConvertRingtoneAsync(outputPath, cancellationToken);
            HasConvertedOutput = true;
            SuccessMessage = "Conversion_SuccessMessage".GetLocalized();
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Conversion_CancelledMessage".GetLocalized();
        }
        catch (Exception exception)
        {
            exception.RethrowIfDebuggerAttached();
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsTranscoding = false;
        }
    }

    public async Task<string?> CreateTemporaryRingtoneAsync(CancellationToken cancellationToken = default)
    {
        if (!HasValidRingtoneSelection())
        {
            ErrorMessage = "Conversion_InvalidSelectionMessage".GetLocalized();
            return null;
        }

        var temporaryDirectory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "iPhoneRingsMaker",
            "Ringtones",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
        var temporaryPath = System.IO.Path.Combine(
            temporaryDirectory,
            $"{await GetSuggestedRingtoneNameAsync()}.m4r");

        try
        {
            ErrorMessage = null;
            SuccessMessage = null;
            TranscodeProgress = 0;
            IsTranscoding = true;
            await ConvertRingtoneAsync(temporaryPath, cancellationToken);
            return temporaryPath;
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Conversion_CancelledMessage".GetLocalized();
        }
        catch (Exception exception)
        {
            exception.RethrowIfDebuggerAttached();
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsTranscoding = false;
        }

        Directory.Delete(temporaryDirectory, recursive: true);
        return null;
    }

    public void DeleteTemporaryRingtone(string path) => _conversionService.DeleteTemporaryOutput(path);

    [RelayCommand]
    private void NavigateToEdition() =>
        _navigationService.NavigateTo(typeof(EditionViewModel).FullName!);

    [RelayCommand]
    private async Task OpenOutputFolderAsync()
    {
        var directory = System.IO.Path.GetDirectoryName(OutputPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        await _folderLauncherService.OpenFolderAsync(directory);
    }

    private async Task ConvertRingtoneAsync(string outputPath, CancellationToken cancellationToken)
    {
        var project = _projectManager.Project ?? throw new InvalidOperationException("No project is open.");
        var media = _media ?? throw new InvalidOperationException("No media is loaded.");
        var source = Source ?? throw new InvalidOperationException("No media preview is available.");
        var durationLimit = source.DurationLimit ?? TimeSpan.Zero;
        var progress = new Progress<double>(value => TranscodeProgress = (int)value);
        await _conversionService.ConvertAsync(
            media,
            project,
            durationLimit,
            outputPath,
            progress,
            cancellationToken);
    }

    private async Task<string> GetSuggestedRingtoneNameAsync()
    {
        var project = _projectManager.Project ?? throw new InvalidOperationException("No project is open.");
        return await _mediaFileNameService.GetSuggestedNameAsync(project.MediaSource, _media);
    }

    private bool CanConvert()
    {
        return HasValidRingtoneSelection()
            && !string.IsNullOrWhiteSpace(OutputPath);
    }

    private bool HasValidRingtoneSelection()
    {
        return _projectManager.IsProjectOpen
            && Source is not null
            && Source.DurationLimit > TimeSpan.Zero
            && Source.DurationLimit <= RingtoneConstraints.MaximumDuration;
    }
    public async void OnNavigatedTo(object parameter)
    {
        var project = _projectManager.Project;
        if (project is not null)
        {
            await ReloadProject(project);
        }
    }
    public void OnNavigatedFrom()
    {

    }
}
