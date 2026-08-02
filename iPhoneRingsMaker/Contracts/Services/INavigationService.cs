using Microsoft.UI.Xaml.Controls;

namespace iPhoneRingsMaker.Contracts.Services;

public interface INavigationService
{
    event EventHandler<NavigationChangedEventArgs> Navigated;

    bool CanGoBack
    {
        get;
    }

    Type? CurrentPageType
    {
        get;
    }

    Frame? Frame
    {
        get; set;
    }

    bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false);

    bool GoBack();
}

public sealed class NavigationChangedEventArgs(Type sourcePageType) : EventArgs
{
    public Type SourcePageType { get; } = sourcePageType;
}
