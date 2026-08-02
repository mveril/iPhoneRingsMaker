using iPhoneRingsMaker.Contracts.Services;

namespace iPhoneRingsMaker.Services;

public sealed class WindowService : IWindowService
{
    private readonly IWindowContext _windowContext;

    public WindowService(IWindowContext windowContext)
    {
        _windowContext = windowContext;
    }

    public void CloseWithoutConfirmation() =>
        _windowContext.CloseWithoutConfirmation();
}
