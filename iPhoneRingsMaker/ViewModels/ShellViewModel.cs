using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Core.Contracts.Services;
using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Helpers;
using iPhoneRingsMaker.Views;
using Microsoft.UI.Xaml.Navigation;
using IProjectMediaSource = iPhoneRingsMaker.Core.Models.IMediaSource;

namespace iPhoneRingsMaker.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    [ObservableProperty]
    public partial bool IsBackEnabled
    {
        get; set;
    }
    private readonly IM4RProjManager M4RProjManager;
    private readonly IFilePickerService _filePickerService;
    private readonly IUserDialogService _userDialogService;
    private readonly IWindowService _windowService;
    private readonly IMediaFileNameService _mediaFileNameService;

    public INavigationService NavigationService
    {
        get;
    }

    public ShellViewModel(
        INavigationService navigationService,
        IM4RProjManager M4RProjManager,
        IFilePickerService filePickerService,
        IUserDialogService userDialogService,
        IWindowService windowService,
        IMediaFileNameService mediaFileNameService)
    {
        NavigationService = navigationService;
        this.M4RProjManager = M4RProjManager;
        _filePickerService = filePickerService;
        _userDialogService = userDialogService;
        _windowService = windowService;
        _mediaFileNameService = mediaFileNameService;
        this.M4RProjManager.ProjectLoaded += OnProjectLoadingChanged;
        this.M4RProjManager.ProjectUnloaded += OnProjectLoadingChanged;
        NavigationService.Navigated += OnNavigated;

    }


    private void OnProjectLoadingChanged(object? sender, ProjectEventArgs e)
    {
        MenuFileSaveAsCommand.NotifyCanExecuteChanged();
        MenuFileSaveCommand.NotifyCanExecuteChanged();
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
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
        var project = M4RProjManager.Project;
        if (project is null)
        {
            return;
        }

        var suggestedName = await _mediaFileNameService.GetSuggestedNameAsync(project.MediaSource);
        var path = await _filePickerService.PickSaveFileAsync("Project file", ".m4rproj", suggestedName);
        if (path is not null)
        {
            await M4RProjManager.SaveProjectAsAsync(path);
        }
    }

    public bool HasProject => M4RProjManager.IsProjectOpen;

    public bool CanOpenMedia => NavigationService.Frame?.CurrentSourcePageType == typeof(EditionPage);

    public void OpenMediaSource(IProjectMediaSource mediaSource)
    {
        ArgumentNullException.ThrowIfNull(mediaSource);
        M4RProjManager.Project = new M4RProj { MediaSource = mediaSource };
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
                await M4RProjManager.OpenProjectAsync(path);
                break;
            default:
                var mediaSource = new LocalMediaSource() { Path = path };
                var proj = new M4RProj() { MediaSource = mediaSource };
                M4RProjManager.Project = proj;
                break;
        }
    }

    private bool CanExecuteOnEdition() => NavigationService.Frame?.CurrentSourcePageType == typeof(EditionPage);

    private async Task<bool> SaveCurrentProjectAsync()
    {
        if (M4RProjManager.IsFileAttached)
        {
            await M4RProjManager.SaveProjectAsync();
            return true;
        }

        var project = M4RProjManager.Project;
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

        await M4RProjManager.SaveProjectAsAsync(path);
        return true;
    }

    public async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (!M4RProjManager.HasUnsavedChanges)
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
