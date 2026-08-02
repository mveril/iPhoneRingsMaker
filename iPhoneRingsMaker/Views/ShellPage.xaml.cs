using System.Diagnostics;
using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Helpers;
using iPhoneRingsMaker.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

using Windows.System;

namespace iPhoneRingsMaker.Views;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel
    {
        get;
    }

    public ShellPage(ShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.NavigationService.Navigated += NavigationService_Navigated;
        MainSelector.SelectedItem = EditionSelectorItem;

        // A custom title bar is required for full window theme and Mica support.
        // https://learn.microsoft.com/windows/apps/develop/title-bar
        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        App.MainWindow.Activated += MainWindow_Activated;
        AppTitleBar.Title = "AppDisplayName".GetLocalized();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);

        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu));
        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));

    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        App.AppTitlebar = AppTitleBar;
    }

    private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
    {
        var keyboardAccelerator = new KeyboardAccelerator() { Key = key };

        if (modifiers.HasValue)
        {
            keyboardAccelerator.Modifiers = modifiers.Value;
        }

        keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;

        return keyboardAccelerator;
    }

    private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var navigationService = App.GetService<INavigationService>();

        var result = navigationService.GoBack();

        args.Handled = result;
    }

    private void MainSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem?.Tag is not string tag)
        {
            return;
        }

        switch (tag)
        {
            case "edition":
                ViewModel.MenuViewsEditionCommand.Execute(null);
                break;
            case "conversion":
                ViewModel.MenuViewsConvertCommand.Execute(null);
                break;
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        MainSelector.SelectedItem = null;
        ViewModel.MenuSettingsCommand.Execute(null);
    }

    private void NavigationService_Navigated(object sender, NavigationEventArgs e)
    {
        MainSelector.SelectedItem = e.SourcePageType switch
        {
            var pageType when pageType == typeof(EditionPage) => EditionSelectorItem,
            var pageType when pageType == typeof(ConversionPage) => MainSelector.Items[1],
            _ => null,
        };
    }
}
