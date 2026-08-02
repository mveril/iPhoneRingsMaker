using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Models;
using iPhoneRingsMaker.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace iPhoneRingsMaker.Views;

public sealed partial class IPhoneMusicPickerDialog : ContentDialog
{
    public IPhoneMusicPickerDialog(IPhoneMusicPickerViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public IPhoneMusicPickerViewModel ViewModel
    {
        get;
    }

    public IPhoneMediaSource? SelectedSource
    {
        get; private set;
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

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        SelectedSource = ViewModel.CreateSelectedSource();
        args.Cancel = SelectedSource is null;
    }
}
