using iPhoneRingsMaker.Activation;
using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Views;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Windows.ApplicationModel.Activation;

namespace iPhoneRingsMaker.Services;

public class ActivationService : IActivationService
{
    private readonly ActivationHandler<ILaunchActivatedEventArgs> _defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly IJumplistService _jumplistService;
    private readonly IWindowContext _windowContext;
    private readonly ShellPage _shell;

    public ActivationService(
        ActivationHandler<ILaunchActivatedEventArgs> defaultHandler,
        IEnumerable<IActivationHandler> activationHandlers,
        IThemeSelectorService themeSelectorService,
        IJumplistService jumplistService,
        IWindowContext windowContext,
        ShellPage shell)
    {
        _defaultHandler = defaultHandler;
        _activationHandlers = activationHandlers;
        _themeSelectorService = themeSelectorService;
        _jumplistService = jumplistService;
        _windowContext = windowContext;
        _shell = shell;
    }

    public async Task ActivateAsync(object activationArgs)
    {
        // Execute tasks before activation.
        await InitializeAsync();
        await _jumplistService.InitializeAsync();

        // Set the MainWindow Content.
        if (_windowContext.Window.Content == null)
        {
            _windowContext.Window.Content = _shell;
        }

        // Handle activation via ActivationHandlers.
        await HandleActivationAsync(activationArgs);

        // Activate the MainWindow.
        _windowContext.Activate();

        // Execute tasks after activation.
        await StartupAsync();
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs));

        if (activationHandler != null)
        {
            await activationHandler.HandleAsync(activationArgs);
        }

        if (_defaultHandler.CanHandle(activationArgs))
        {
            await _defaultHandler.HandleAsync(activationArgs);
        }
    }

    private async Task InitializeAsync()
    {
        await _themeSelectorService.InitializeAsync().ConfigureAwait(false);
        await Task.CompletedTask;
    }

    private async Task StartupAsync()
    {
        await _themeSelectorService.SetRequestedThemeAsync();
        await Task.CompletedTask;
    }
}
