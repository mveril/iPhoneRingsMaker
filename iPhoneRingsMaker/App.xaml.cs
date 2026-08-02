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

namespace iPhoneRingsMaker;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host
    {
        get;
    }

    public static T GetService<T>()
        where T : class
    {
        return (App.Current as App)!.Host.Services.GetRequiredService<T>();
    }

    public static Window MainWindow { get; } = new MainWindow();

    public static UIElement? AppTitlebar
    {
        get; set;
    }

    public App()
    {
        InitializeComponent();

        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory,
        });

        builder.Services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();
        builder.Services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
        builder.Services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
        builder.Services.AddSingleton<IActivationService, ActivationService>();
        builder.Services.AddSingleton<IPageService, PageService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IM4RProjManager, M4RProjManager>();
        builder.Services.AddSingleton<IFileService, FileService>();
        builder.Services.AddSingleton<IRingtoneTransferAdapter, AfcRingtoneTransferAdapter>();
        builder.Services.AddSingleton<IAppleDeviceService, NetimobiledeviceAppleDeviceService>();
        builder.Services.AddSingleton<iPhoneRingsMaker.Core.Services.IPhoneMusicCatalogParser>();
        builder.Services.AddSingleton<IAppleMusicLibraryService, AppleMusicLibraryService>();
        builder.Services.AddSingleton<IFilePickerService, FilePickerService>();
        builder.Services.AddSingleton<IFolderLauncherService, FolderLauncherService>();
        builder.Services.AddSingleton<IUserDialogService, UserDialogService>();
        builder.Services.AddSingleton<IWindowService, WindowService>();
        builder.Services.AddSingleton<IMediaFileNameService, MediaFileNameService>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<EditionViewModel>();
        builder.Services.AddTransient<EditionPage>();
        builder.Services.AddTransient<ConversionPage>();
        builder.Services.AddTransient<ConversionViewModel>();
        builder.Services.AddTransient<IPhoneMusicPickerViewModel>();
        builder.Services.AddTransient<ShellPage>();
        builder.Services.AddTransient<ShellViewModel>();
        builder.Services.Configure<LocalSettingsOptions>(
            builder.Configuration.GetSection(nameof(LocalSettingsOptions)));

        Host = builder.Build();

        UnhandledException += App_UnhandledException;
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

        if (MainWindow.Content is FrameworkElement root)
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

    private async void OpenIPhoneMusicCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        var shellViewModel = GetService<ShellViewModel>();
        if (shellViewModel.HasProject && !await shellViewModel.ConfirmDiscardChangesAsync())
        {
            return;
        }

        if (MainWindow.Content is not FrameworkElement root)
        {
            return;
        }

        var dialog = new IPhoneMusicPickerDialog(GetService<IPhoneMusicPickerViewModel>())
        {
            XamlRoot = root.XamlRoot,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary
            && dialog.SelectedSource is not null)
        {
            if (shellViewModel.NavigationService.Frame?.Content is EditionPage editionPage)
            {
                await editionPage.ViewModel.OpenMediaSourceAsync(dialog.SelectedSource);
            }
            else
            {
                shellViewModel.OpenMediaSource(dialog.SelectedSource);
            }
        }
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        ((StandardUICommand)Resources["OpenCommand"])
            .Command = GetService<ShellViewModel>().MenuFileOpenCommand;
        ((XamlUICommand)Resources["OpenIPhoneMusicCommand"])
            .ExecuteRequested += OpenIPhoneMusicCommand_ExecuteRequested;

        await App.GetService<IActivationService>().ActivateAsync(args);
    }
}
