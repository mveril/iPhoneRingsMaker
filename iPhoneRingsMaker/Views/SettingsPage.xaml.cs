using iPhoneRingsMaker.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace iPhoneRingsMaker.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel
    {
        get;
    }

    public SettingsPage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private async void SkipIntervalNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!double.IsNaN(args.NewValue))
        {
            await ViewModel.SetSkipIntervalSecondsAsync(args.NewValue);
        }
    }
}
