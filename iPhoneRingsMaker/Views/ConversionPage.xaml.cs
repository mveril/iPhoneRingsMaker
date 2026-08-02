using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.ComponentModel;
using iPhoneRingsMaker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace iPhoneRingsMaker.Views;

public sealed partial class ConversionPage : Page
{
    public ConversionViewModel ViewModel
    {
        get;
    }

    public ConversionPage()
    {
        ViewModel = App.GetService<ConversionViewModel>();
        InitializeComponent();
    }

    private async void OnTransferToIPhoneClick(object sender, RoutedEventArgs e)
    {
        var temporaryPath = await ViewModel.CreateTemporaryRingtoneAsync();
        if (string.IsNullOrWhiteSpace(temporaryPath))
        {
            return;
        }

        try
        {
            var dialog = new DevicePickerPage(new DevicePickerViewModel(
                App.GetService<Contracts.Services.IAppleDeviceService>(),
                temporaryPath))
            {
                XamlRoot = XamlRoot,
            };

            await dialog.ShowAsync();
        }
        finally
        {
            ConversionViewModel.DeleteTemporaryRingtone(temporaryPath);
        }
    }
}
