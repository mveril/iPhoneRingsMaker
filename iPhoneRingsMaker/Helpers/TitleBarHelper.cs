using System.Runtime.InteropServices;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace iPhoneRingsMaker.Helpers;

// Helper class that works around custom title bar limitations.
// DISCLAIMER: The resource key names and color values used below are subject to change. Do not depend on them.
internal class TitleBarHelper
{
    private const int WAINACTIVE = 0x00;
    private const int WAACTIVE = 0x01;
    private const int WMACTIVATE = 0x0006;

    public static void UpdateTitleBar(Window window, ElementTheme theme)
    {
        if (window.ExtendsContentIntoTitleBar)
        {
            if (theme == ElementTheme.Default)
            {
                var uiSettings = new UISettings();
                var background = uiSettings.GetColorValue(UIColorType.Background);

                theme = background == Colors.White ? ElementTheme.Light : ElementTheme.Dark;
            }

            if (theme == ElementTheme.Default)
            {
                theme = Application.Current.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark;
            }

            window.AppWindow.TitleBar.ButtonForegroundColor = theme switch
            {
                ElementTheme.Dark => Colors.White,
                ElementTheme.Light => Colors.Black,
                _ => Colors.Transparent
            };

            window.AppWindow.TitleBar.ButtonHoverForegroundColor = theme switch
            {
                ElementTheme.Dark => Colors.White,
                ElementTheme.Light => Colors.Black,
                _ => Colors.Transparent
            };

            window.AppWindow.TitleBar.ButtonHoverBackgroundColor = theme switch
            {
                ElementTheme.Dark => Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF),
                ElementTheme.Light => Color.FromArgb(0x33, 0x00, 0x00, 0x00),
                _ => Colors.Transparent
            };

            window.AppWindow.TitleBar.ButtonPressedBackgroundColor = theme switch
            {
                ElementTheme.Dark => Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF),
                ElementTheme.Light => Color.FromArgb(0x66, 0x00, 0x00, 0x00),
                _ => Colors.Transparent
            };

            window.AppWindow.TitleBar.BackgroundColor = Colors.Transparent;

            var hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(window));
            if (hwnd == PInvoke.GetActiveWindow())
            {
                PInvoke.SendMessage(hwnd, WMACTIVATE, new WPARAM(WAINACTIVE), default);
                PInvoke.SendMessage(hwnd, WMACTIVATE, new WPARAM(WAACTIVE), default);
            }
            else
            {
                PInvoke.SendMessage(hwnd, WMACTIVATE, new WPARAM(WAACTIVE), default);
                PInvoke.SendMessage(hwnd, WMACTIVATE, new WPARAM(WAINACTIVE), default);
            }
        }
    }

    public static void ApplySystemThemeToCaptionButtons(Window window, FrameworkElement? titleBar)
    {
        if (titleBar is not null)
        {
            UpdateTitleBar(window, titleBar.ActualTheme);
        }
    }
}
