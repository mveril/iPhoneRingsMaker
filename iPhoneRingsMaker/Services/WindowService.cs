using iPhoneRingsMaker.Contracts.Services;

namespace iPhoneRingsMaker.Services;

public sealed class WindowService : IWindowService
{
    public void CloseWithoutConfirmation() =>
        ((MainWindow)App.MainWindow).CloseWithoutConfirmation();
}
