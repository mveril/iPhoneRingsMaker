using iPhoneRingsMaker.Helpers;

using Windows.UI.ViewManagement;

namespace iPhoneRingsMaker;

public sealed partial class MainWindow : Microsoft.UI.Xaml.Window
{
    private bool _closeConfirmed;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
    private readonly UISettings _settings;

    public Func<Task<bool>>? ConfirmCloseAsync
    {
        get; set;
    }

    public Microsoft.UI.Xaml.FrameworkElement? AppTitleBar
    {
        get; set;
    }

    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "AppDisplayName".GetLocalized();
        AppWindow.Closing += AppWindow_Closing;

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _settings = new UISettings();
        _settings.ColorValuesChanged += Settings_ColorValuesChanged;
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

        if (ConfirmCloseAsync is null)
        {
            return;
        }

        args.Cancel = true;
        if (await ConfirmCloseAsync())
        {
            CloseWithoutConfirmation();
        }
    }

    // Keep caption button colors synchronized when the Windows theme changes.
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            TitleBarHelper.ApplySystemThemeToCaptionButtons(this, AppTitleBar);
        });
    }
}
