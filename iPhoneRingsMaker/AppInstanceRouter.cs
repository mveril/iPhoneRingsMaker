using iPhoneRingsMaker.Services;

using Microsoft.Windows.AppLifecycle;

using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace iPhoneRingsMaker;

internal static class AppInstanceRouter
{
    public static AppInstance FindOrRegister(AppActivationArguments activationArguments)
    {
        ArgumentNullException.ThrowIfNull(activationArguments);

        if (activationArguments.Data is not IFileActivatedEventArgs fileArgs)
        {
            return AppInstance.GetCurrent();
        }

        var projectFile = fileArgs.Files
            .OfType<StorageFile>()
            .FirstOrDefault(static file =>
                string.Equals(file.FileType, ".m4rproj", StringComparison.OrdinalIgnoreCase));

        return projectFile is null
            ? AppInstance.GetCurrent()
            : AppInstance.FindOrRegisterForKey(ProjectInstanceRegistry.GetKey(projectFile.Path));
    }
}
