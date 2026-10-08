using iPhoneRingsMaker.Contracts.Services;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace iPhoneRingsMaker.Services;

public sealed class WindowContext : IWindowContext
{
    private readonly MainWindow _window;

    public WindowContext(MainWindow window)
    {
        _window = window;
    }

    public Window Window => _window;

    public AppWindow AppWindow => _window.AppWindow;

    public FrameworkElement? Root => _window.Content as FrameworkElement;

    public FrameworkElement? TitleBar
    {
        get => _window.AppTitleBar;
        set => _window.AppTitleBar = value;
    }

    public void Activate()
    {
        _window.Activate();

        var hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(_window));
        PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);
        PInvoke.SetForegroundWindow(hwnd);
    }

    public void CloseWithoutConfirmation() => _window.CloseWithoutConfirmation();
}
