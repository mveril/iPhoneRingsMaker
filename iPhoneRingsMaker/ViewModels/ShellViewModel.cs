using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Core.Contracts.Services;
using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Helpers;
using iPhoneRingsMaker.Views;
using IProjectMediaSource = iPhoneRingsMaker.Core.Models.IMediaSource;

namespace iPhoneRingsMaker.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    [ObservableProperty]
    public partial bool IsBackEnabled
    {
        get; set;
    }
    private readonly IM4RProjectManager _projectManager;
    private readonly IFilePickerService _filePickerService;
    private readonly IUserDialogService _userDialogService;
    private readonly IWindowService _windowService;
    private readonly IMediaFileNameService _mediaFileNameService;
    private readonly IM4RProjectFactory _projectFactory;
    private readonly IPhoneMusicSelectionService _iPhoneMusicSelectionService;

    public INavigationService NavigationService
    {
        get;
    }

    public ShellViewModel(
        INavigationService navigationService,
        IM4RProjectManager projectManager,
        IFilePickerService filePickerService,
        IUserDialogService userDialogService,
        IWindowService windowService,
        IMediaFileNameService mediaFileNameService,
        IM4RProjectFactory projectFactory,
        IPhoneMusicSelectionService iPhoneMusicSelectionService)
    {
        NavigationService = navigationService;
        _projectManager = projectManager;
        _filePickerService = filePickerService;
        _userDialogService = userDialogService;
        _windowService = windowService;
        _mediaFileNameService = mediaFileNameService;
        _projectFactory = projectFactory;
        _iPhoneMusicSelectionService = iPhoneMusicSelectionService;
        _projectManager.ProjectLoaded += OnProjectLoadingChanged;
        _projectManager.ProjectUnloaded += OnProjectLoadingChanged;
        NavigationService.Navigated += OnNavigated;

    }


    private void OnProjectLoadingChanged(object? sender, ProjectEventArgs e)
    {
        MenuFileSaveAsCommand.NotifyCanExecuteChanged();
        MenuFileSaveCommand.NotifyCanExecuteChanged();
    }

    private void OnNavigated(object? sender, NavigationChangedEventArgs e)
    {
        IsBackEnabled = NavigationService.CanGoBack;
        OnPropertyChanged(nameof(CanOpenMedia));
        MenuFileOpenCommand.NotifyCanExecuteChanged();

    }
    [RelayCommand]
    private async Task MenuFileExitAsync()
    {
        if (await ConfirmDiscardChangesAsync())
        {
            _windowService.CloseWithoutConfirmation();
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(HasProject))]
    private async Task OnMenuFileSave()
    {
        await SaveCurrentProjectAsync();
    }

    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(HasProject))]
    private async Task OnMenuFileSaveAsAsync()
    {
        var project = _projectManager.Project;
        if (project is null)
        {
            return;
        }

        var suggestedName = await _mediaFileNameService.GetSuggestedNameAsync(project.MediaSource);
        var path = await _filePickerService.PickSaveFileAsync("Project file", ".m4rproj", suggestedName);
        if (path is not null)
        {
            await _projectManager.SaveProjectAsAsync(path);
        }
    }

    public bool HasProject => _projectManager.IsProjectOpen;

    public bool CanOpenMedia => NavigationService.CurrentPageType == typeof(EditionPage);

    public void OpenMediaSource(IProjectMediaSource mediaSource)
    {
        ArgumentNullException.ThrowIfNull(mediaSource);
        _projectManager.Project = _projectFactory.Create(mediaSource);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task OpenIPhoneMusicAsync()
    {
        if (HasProject && !await ConfirmDiscardChangesAsync())
        {
            return;
        }

        var source = await _iPhoneMusicSelectionService.PickAsync();
        if (source is null)
        {
            return;
        }

        if (NavigationService.Frame?.Content is EditionPage editionPage)
        {
            await editionPage.ViewModel.OpenMediaSourceAsync(source);
        }
        else
        {
            OpenMediaSource(source);
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteOnEdition), AllowConcurrentExecutions = false)]
    private async Task OnMenuFileOpen()
    {
        if (!await ConfirmDiscardChangesAsync())
        {
            return;
        }

        string[] fileTypes = [.. LocalMediaSource.SupportedFileTypes, ".m4rproj"];
        var path = await _filePickerService.PickOpenFileAsync(fileTypes);
        if (path is null)
        {
            return;
        }
        switch (System.IO.Path.GetExtension(path).ToLowerInvariant())
        {
            case ".m4rproj":
                await _projectManager.OpenProjectAsync(path);
                break;
            default:
                var mediaSource = new LocalMediaSource() { Path = path };
                _projectManager.Project = _projectFactory.Create(mediaSource);
                break;
        }
    }

    private bool CanExecuteOnEdition() => NavigationService.CurrentPageType == typeof(EditionPage);

    private async Task<bool> SaveCurrentProjectAsync()
    {
        if (_projectManager.IsFileAttached)
        {
            await _projectManager.SaveProjectAsync();
            return true;
        }

        var project = _projectManager.Project;
        if (project is null)
        {
            return false;
        }

        var suggestedName = await _mediaFileNameService.GetSuggestedNameAsync(project.MediaSource);
        var path = await _filePickerService.PickSaveFileAsync("Project file", ".m4rproj", suggestedName);
        if (path is null)
        {
            return false;
        }

        return await _projectManager.SaveProjectAsAsync(path);
    }

    public async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (!_projectManager.HasUnsavedChanges)
        {
            return true;
        }

        return await _userDialogService.ConfirmUnsavedChangesAsync() switch
        {
            UnsavedChangesChoice.Save => await SaveCurrentProjectAsync(),
            UnsavedChangesChoice.Discard => true,
            _ => false,
        };
    }

    [RelayCommand]
    private void MenuSettings() => NavigationService.NavigateTo(typeof(SettingsViewModel).FullName!);

    [RelayCommand]
    private void MenuViewsEdition() => NavigationService.NavigateTo(typeof(EditionViewModel).FullName!);

    [RelayCommand]
    private void MenuViewsConvert() => NavigationService.NavigateTo(typeof(ConversionViewModel).FullName!);
}
