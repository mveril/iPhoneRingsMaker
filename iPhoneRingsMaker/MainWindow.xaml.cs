using iPhoneRingsMaker.Helpers;

using Windows.UI.ViewManagement;

namespace iPhoneRingsMaker;

public sealed partial class MainWindow : Microsoft.UI.Xaml.Window
{
    private bool _closeConfirmed;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue;

    private readonly UISettings settings;

    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "AppDisplayName".GetLocalized();
        AppWindow.Closing += AppWindow_Closing;

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event
    }

    public void CloseWithoutConfirmation()
    {
        _closeConfirmed = true;
        Close();
    }

    private async void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (_closeConfirmed)
        {
            return;
        }

        var projectManager = App.GetService<iPhoneRingsMaker.Core.Contracts.Services.IM4RProjManager>();
        if (!projectManager.HasUnsavedChanges)
        {
            return;
        }

        args.Cancel = true;
        var shellViewModel = App.GetService<ViewModels.ShellViewModel>();
        if (await shellViewModel.ConfirmDiscardChangesAsync())
        {
            CloseWithoutConfirmation();
        }
    }

    // this handles updating the caption button colors correctly when indows system theme is changed
    // while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        dispatcherQueue.TryEnqueue(() =>
        {
            TitleBarHelper.ApplySystemThemeToCaptionButtons();
        });
    }
}
