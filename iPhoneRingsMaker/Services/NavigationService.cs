using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Contracts.ViewModels;
using iPhoneRingsMaker.Helpers;

using Microsoft.UI.Xaml.Controls;

namespace iPhoneRingsMaker.Services;

public class NavigationService(IPageService pageService, IWindowContext windowContext) : INavigationService
{
    private readonly Stack<(string PageKey, object? Parameter)> _backStack = new();
    private object? _lastParameterUsed;
    private Frame? _frame;

    public event EventHandler<NavigationChangedEventArgs>? Navigated;

    public bool CanGoBack => _backStack.Count > 0;

    public Type? CurrentPageType => Frame?.Content?.GetType();

    public Frame? Frame
    {
        get => _frame ??= windowContext.Window.Content as Frame;
        set => _frame = value;
    }

    public bool GoBack()
    {
        if (!CanGoBack || Frame is null)
        {
            return false;
        }

        var currentViewModel = Frame.GetPageViewModel();
        var destination = _backStack.Pop();
        Frame.Content = pageService.CreatePage(destination.PageKey);
        (currentViewModel as INavigationAware)?.OnNavigatedFrom();
        _lastParameterUsed = destination.Parameter;
        NotifyNavigated(destination.Parameter);
        return true;
    }

    public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
    {
        if (Frame is null)
        {
            return false;
        }

        var pageType = pageService.GetPageType(pageKey);
        if (Frame.Content?.GetType() == pageType
            && (parameter is null || parameter.Equals(_lastParameterUsed)))
        {
            return false;
        }

        var currentViewModel = Frame.GetPageViewModel();
        if (!clearNavigation && currentViewModel?.GetType().FullName is string currentPageKey)
        {
            _backStack.Push((currentPageKey, _lastParameterUsed));
        }

        Frame.Content = pageService.CreatePage(pageKey);
        (currentViewModel as INavigationAware)?.OnNavigatedFrom();
        _lastParameterUsed = parameter;
        if (clearNavigation)
        {
            _backStack.Clear();
        }

        NotifyNavigated(parameter);
        return true;
    }

    private void NotifyNavigated(object? parameter)
    {
        (Frame?.GetPageViewModel() as INavigationAware)?.OnNavigatedTo(parameter!);
        if (CurrentPageType is not null)
        {
            Navigated?.Invoke(this, new NavigationChangedEventArgs(CurrentPageType));
        }
    }
}
