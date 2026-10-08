using System.Diagnostics;
using System.Runtime.InteropServices;

using iPhoneRingsMaker.Core.Contracts.Services;

using Microsoft.Windows.AppLifecycle;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace iPhoneRingsMaker.Services;

internal sealed class ProjectInstanceRegistry : IProjectInstanceRegistry
{
    private string? _currentKey = NormalizeKey(AppInstance.GetCurrent().Key);

    public bool TryClaim(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var requestedKey = GetKey(path);
        if (string.Equals(_currentKey, requestedKey, StringComparison.Ordinal))
        {
            return true;
        }

        var previousKey = _currentKey;
        if (previousKey is not null)
        {
            AppInstance.GetCurrent().UnregisterKey();
        }

        _currentKey = null;

        var targetInstance = AppInstance.FindOrRegisterForKey(requestedKey);
        if (targetInstance.IsCurrent)
        {
            _currentKey = requestedKey;
            return true;
        }

        if (previousKey is not null)
        {
            var restoredInstance = AppInstance.FindOrRegisterForKey(previousKey);
            if (restoredInstance.IsCurrent)
            {
                _currentKey = previousKey;
            }
            else
            {
                throw new InvalidOperationException("The previous project instance key could not be restored.");
            }
        }

        Activate(targetInstance.ProcessId);
        return false;
    }

    public void Release()
    {
        if (_currentKey is null)
        {
            return;
        }

        AppInstance.GetCurrent().UnregisterKey();
        _currentKey = null;
    }

    internal static string GetKey(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return $"m4rproj:{Path.GetFullPath(path).ToUpperInvariant()}";
    }

    private static string? NormalizeKey(string key) =>
        string.IsNullOrEmpty(key) ? null : key;

    private static void Activate(uint processId)
    {
        try
        {
            using var process = Process.GetProcessById(checked((int)processId));
            var window = process.MainWindowHandle;
            if (window == IntPtr.Zero)
            {
                return;
            }

            var hwnd = new HWND(window);
            PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);
            PInvoke.SetForegroundWindow(hwnd);
        }
        catch (ArgumentException)
        {
        }
    }

}
