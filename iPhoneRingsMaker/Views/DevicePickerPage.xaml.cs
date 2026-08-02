using iPhoneRingsMaker.Models;
using iPhoneRingsMaker.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace iPhoneRingsMaker.Views;

public sealed partial class DevicePickerPage : ContentDialog
{
    public DevicePickerPage(DevicePickerViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public DevicePickerViewModel ViewModel
    {
        get;
    }

    public InfoBarSeverity ToInfoBarSeverity(PickerStatusSeverity severity) => severity switch
    {
        PickerStatusSeverity.Success => InfoBarSeverity.Success,
        PickerStatusSeverity.Warning => InfoBarSeverity.Warning,
        PickerStatusSeverity.Error => InfoBarSeverity.Error,
        _ => InfoBarSeverity.Informational,
    };

    private async void OnLoaded(object sender, RoutedEventArgs e) => await ViewModel.ActivateAsync();

    private void OnUnloaded(object sender, RoutedEventArgs e) => ViewModel.Deactivate();

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;
        var deferral = args.GetDeferral();
        try
        {
            if (await ViewModel.TransferAsync())
            {
                Hide();
            }
        }
        finally
        {
            deferral.Complete();
        }
    }
}
