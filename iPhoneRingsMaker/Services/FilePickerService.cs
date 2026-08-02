using iPhoneRingsMaker.Contracts.Services;

using Microsoft.Windows.Storage.Pickers;

namespace iPhoneRingsMaker.Services;

public sealed class FilePickerService : IFilePickerService
{
    private readonly IWindowContext _windowContext;

    public FilePickerService(IWindowContext windowContext)
    {
        _windowContext = windowContext;
    }

    public async Task<string?> PickOpenFileAsync(IReadOnlyCollection<string> fileTypes)
    {
        var picker = new FileOpenPicker(_windowContext.AppWindow.Id);
        foreach (var fileType in fileTypes)
        {
            picker.FileTypeFilter.Add(fileType);
        }

        return (await picker.PickSingleFileAsync())?.Path;
    }

    public async Task<string?> PickSaveFileAsync(string fileTypeLabel, string extension, string suggestedFileName)
    {
        var picker = new FileSavePicker(_windowContext.AppWindow.Id);
        picker.FileTypeChoices.Add(fileTypeLabel, [extension]);
        picker.SuggestedFileName = suggestedFileName;
        return (await picker.PickSaveFileAsync())?.Path;
    }
}
