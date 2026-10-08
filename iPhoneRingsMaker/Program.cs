using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace iPhoneRingsMaker;

internal static class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        var currentInstance = AppInstance.GetCurrent();
        var activationArguments = currentInstance.GetActivatedEventArgs();

        var targetInstance = AppInstanceRouter.FindOrRegister(
            activationArguments);

        if (!targetInstance.IsCurrent)
        {
            await targetInstance.RedirectActivationToAsync(
                activationArguments);

            return;
        }

        Application.Start(_args =>
        {
            var dispatcherQueue =
                DispatcherQueue.GetForCurrentThread();

            SynchronizationContext.SetSynchronizationContext(
                new DispatcherQueueSynchronizationContext(
                    dispatcherQueue));

            _ = new App(activationArguments);
        });
    }
}