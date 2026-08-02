using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.ViewModels;
using iPhoneRingsMaker.Views;

using Microsoft.UI.Xaml.Controls;

namespace iPhoneRingsMaker.Services;

public sealed class PhoneMusicSelectionService(
    IWindowContext windowContext,
    IAppleDeviceService deviceService,
    IAppleMusicLibraryService musicLibraryService) : IPhoneMusicSelectionService
{
    public async Task<IPhoneMediaSource?> PickAsync()
    {
        var root = windowContext.Root;
        if (root?.XamlRoot is null)
        {
            return null;
        }

        using var viewModel = new IPhoneMusicPickerViewModel(deviceService, musicLibraryService);
        var dialog = new IPhoneMusicPickerDialog(viewModel)
        {
            XamlRoot = root.XamlRoot,
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary
            ? dialog.SelectedSource
            : null;
    }
}
