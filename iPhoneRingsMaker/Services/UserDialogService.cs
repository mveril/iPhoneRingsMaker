using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Helpers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace iPhoneRingsMaker.Services;

public sealed class UserDialogService : IUserDialogService
{
    private readonly IWindowContext _windowContext;

    public UserDialogService(IWindowContext windowContext)
    {
        _windowContext = windowContext;
    }

    public async Task<UnsavedChangesChoice> ConfirmUnsavedChangesAsync()
    {
        if (_windowContext.Root is not { } root)
        {
            return UnsavedChangesChoice.Cancel;
        }

        var dialog = new ContentDialog
        {
            Title = "UnsavedChanges_Title".GetLocalized(),
            Content = "UnsavedChanges_Message".GetLocalized(),
            PrimaryButtonText = "UnsavedChanges_Save".GetLocalized(),
            SecondaryButtonText = "UnsavedChanges_Discard".GetLocalized(),
            CloseButtonText = "UnsavedChanges_Cancel".GetLocalized(),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = root.XamlRoot,
        };

        return await dialog.ShowAsync() switch
        {
            ContentDialogResult.Primary => UnsavedChangesChoice.Save,
            ContentDialogResult.Secondary => UnsavedChangesChoice.Discard,
            _ => UnsavedChangesChoice.Cancel,
        };
    }
}
