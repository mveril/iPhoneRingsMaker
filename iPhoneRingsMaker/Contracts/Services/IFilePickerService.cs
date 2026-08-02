namespace iPhoneRingsMaker.Contracts.Services;

public interface IFilePickerService
{
    Task<string?> PickOpenFileAsync(IReadOnlyCollection<string> fileTypes);

    Task<string?> PickSaveFileAsync(string fileTypeLabel, string extension, string suggestedFileName);
}
