namespace iPhoneRingsMaker.Contracts.Services;

public interface IFolderLauncherService
{
    Task<bool> OpenFolderAsync(string path);

    Task<bool> ShowFileAsync(string path);
}
