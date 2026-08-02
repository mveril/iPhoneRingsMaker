using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace iPhoneRingsMaker.Contracts.Services;

public interface IWindowContext
{
    Window Window
    {
        get;
    }

    AppWindow AppWindow
    {
        get;
    }

    FrameworkElement? Root
    {
        get;
    }

    FrameworkElement? TitleBar
    {
        get; set;
    }

    void Activate();

    void CloseWithoutConfirmation();
}
