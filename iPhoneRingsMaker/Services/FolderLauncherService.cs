using iPhoneRingsMaker.Contracts.Services;

using Windows.Storage;

namespace iPhoneRingsMaker.Services;

public sealed class FolderLauncherService : IFolderLauncherService
{
    public async Task<bool> OpenFolderAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        var folder = await StorageFolder.GetFolderFromPathAsync(path);
        return await Windows.System.Launcher.LaunchFolderAsync(folder);
    }

    public async Task<bool> ShowFileAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        var file = await StorageFile.GetFileFromPathAsync(path);
        var folder = await file.GetParentAsync();
        var options = new Windows.System.FolderLauncherOptions();
        options.ItemsToSelect.Add(file);
        return await Windows.System.Launcher.LaunchFolderAsync(folder, options);
    }
}
