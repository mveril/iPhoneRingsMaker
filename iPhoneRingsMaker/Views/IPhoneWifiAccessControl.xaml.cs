using iPhoneRingsMaker.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace iPhoneRingsMaker.Views;

public sealed partial class IPhoneWifiAccessControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(IPhoneWifiAccessViewModel),
        typeof(IPhoneWifiAccessControl),
        new PropertyMetadata(null, OnViewModelChanged));

    public IPhoneWifiAccessControl()
    {
        InitializeComponent();
    }

    public IPhoneWifiAccessViewModel? ViewModel
    {
        get => (IPhoneWifiAccessViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((IPhoneWifiAccessControl)sender).DataContext = args.NewValue;
    }

    private async void OnWifiToggleToggled(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { } viewModel && WifiToggle.IsOn != viewModel.IsWifiEnabled)
        {
            await viewModel.SetWifiEnabledAsync(WifiToggle.IsOn);
        }
    }
}
