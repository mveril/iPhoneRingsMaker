using iPhoneRingsMaker.Contracts.Services;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

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

    public void Activate() => _window.Activate();

    public void CloseWithoutConfirmation() => _window.CloseWithoutConfirmation();
}
