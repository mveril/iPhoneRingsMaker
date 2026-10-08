using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.ViewModels;

using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Activation;

namespace iPhoneRingsMaker.Activation;

public class DefaultActivationHandler : ActivationHandler<ILaunchActivatedEventArgs>
{
    private readonly INavigationService _navigationService;

    public DefaultActivationHandler(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    protected override bool CanHandleInternal(ILaunchActivatedEventArgs args)
    {
        // None of the ActivationHandlers has handled the activation.
        return _navigationService.Frame?.Content == null;
    }

    protected async override Task HandleInternalAsync(ILaunchActivatedEventArgs args)
    {
        _navigationService.NavigateTo(typeof(EditionViewModel).FullName!, args.Arguments);

        await Task.CompletedTask;
    }
}
