using System.Diagnostics;
using iPhoneRingsMaker.Activation;
using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Core.Contracts.Services;
using iPhoneRingsMaker.Core.Services;
using iPhoneRingsMaker.Models;
using iPhoneRingsMaker.Services;
using iPhoneRingsMaker.ViewModels;
using iPhoneRingsMaker.Views;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.AppLifecycle;

namespace iPhoneRingsMaker;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    private readonly MainWindow _mainWindow;
    private readonly AppActivationArguments _activationArguments;

    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host
    {
        get;
    }

    private T GetService<T>()
        where T : class
    {
        return Host.Services.GetRequiredService<T>();
    }

    public App()
        : this(AppInstance.GetCurrent().GetActivatedEventArgs())
    {
    }

    internal App(AppActivationArguments activationArguments)
    {
        _activationArguments = activationArguments;
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory,
        });

        builder.Services.AddTransient<IActivationHandler, M4RProjActivationHandler>();
        builder.Services.AddTransient<ActivationHandler<Windows.ApplicationModel.Activation.ILaunchActivatedEventArgs>, DefaultActivationHandler>();
        builder.Services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
        builder.Services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
        builder.Services.AddSingleton<IJumplistService, JumpListService>();
        builder.Services.AddSingleton<IActivationService, ActivationService>();
        builder.Services.AddSingleton<IPageService, PageService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IProjectInstanceRegistry, ProjectInstanceRegistry>();
        builder.Services.AddSingleton<IM4RProjectManager, M4RProjectManager>();
        builder.Services.AddSingleton<IM4RProjectFactory, M4RProjectFactory>();
        builder.Services.AddSingleton<IFileService, FileService>();
        builder.Services.AddSingleton<IRingtoneTransferAdapter, AfcRingtoneTransferAdapter>();
        builder.Services.AddSingleton<IAppleDeviceService, NetimobiledeviceAppleDeviceService>();
        builder.Services.AddSingleton<iPhoneRingsMaker.Core.Services.IPhoneMusicCatalogParser>();
        builder.Services.AddSingleton<IAppleMusicLibraryService, AppleMusicLibraryService>();
        builder.Services.AddSingleton<IAppleDeviceFileService, AppleDeviceFileService>();
        builder.Services.AddSingleton<IFilePickerService, FilePickerService>();
        builder.Services.AddSingleton<IFolderLauncherService, FolderLauncherService>();
        builder.Services.AddSingleton<IUserDialogService, UserDialogService>();
        builder.Services.AddSingleton<IWindowService, WindowService>();
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddSingleton<IWindowContext, WindowContext>();
        builder.Services.AddSingleton<IMediaFileNameService, MediaFileNameService>();
        builder.Services.AddSingleton<IMediaFactory, MediaFactory>();
        builder.Services.AddSingleton<IRingtoneConversionService, RingtoneConversionService>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<EditionViewModel>();
        builder.Services.AddTransient<EditionPage>();
        builder.Services.AddTransient<ConversionPage>();
        builder.Services.AddTransient<ConversionViewModel>();
        builder.Services.AddTransient<IPhoneMusicSelectionService, PhoneMusicSelectionService>();
        builder.Services.AddTransient<ShellPage>();
        builder.Services.AddSingleton<ShellViewModel>();
        builder.Services.Configure<LocalSettingsOptions>(
            builder.Configuration.GetSection(nameof(LocalSettingsOptions)));

        Host = builder.Build();
        InitializeComponent();
        _mainWindow = GetService<MainWindow>();
        _mainWindow.ConfirmCloseAsync = GetService<ShellViewModel>().ConfirmDiscardChangesAsync;

        AppInstance.GetCurrent().Activated += OnActivated;
        UnhandledException += App_UnhandledException;
    }

    private void OnActivated(object? sender, AppActivationArguments args)
    {
        _mainWindow.DispatcherQueue.TryEnqueue(
            async () => await GetService<IActivationService>().ActivateAsync(args.Data));
    }

    private async void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
#if DEBUG
        // When debugging, keep the exception unhandled so Visual Studio breaks on the
        // original throw site instead of replacing it with the fatal-error dialog.
        if (Debugger.IsAttached)
        {
            return;
        }
#endif

        if (_mainWindow.Content is FrameworkElement root)
        {
            var dialog = new ContentDialog
            {
                Title = "An error occurred",
                Content = e.Message,
                CloseButtonText = "OK",
                XamlRoot = root.XamlRoot,
            };
            await dialog.ShowAsync();
        }

        // In normal runs the exception is handled by the user-facing dialog.
        e.Handled = true;
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        ((StandardUICommand)Resources["OpenCommand"])
            .Command = GetService<ShellViewModel>().MenuFileOpenCommand;
        ((XamlUICommand)Resources["OpenIPhoneMusicCommand"])
            .Command = GetService<ShellViewModel>().OpenIPhoneMusicCommand;

        await GetService<IActivationService>().ActivateAsync(_activationArguments.Data);
    }
}
