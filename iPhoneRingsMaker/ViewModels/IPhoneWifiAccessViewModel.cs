using CommunityToolkit.Mvvm.ComponentModel;

using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Helpers;
using iPhoneRingsMaker.Models;

namespace iPhoneRingsMaker.ViewModels;

public partial class IPhoneWifiAccessViewModel : ObservableObject, IDisposable
{
    private readonly IAppleDeviceService _deviceService;
    private CancellationTokenSource? _selectionCancellation;
    private AppleDeviceInfo? _selectedDevice;
    private bool _isStateKnown;

    public IPhoneWifiAccessViewModel(IAppleDeviceService deviceService)
    {
        _deviceService = deviceService;
        GuidanceMessage = "WifiAccess_SelectDevice".GetLocalized();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StateDescription))]
    public partial bool IsWifiEnabled
    {
        get; set;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanToggle))]
    [NotifyPropertyChangedFor(nameof(StateDescription))]
    public partial bool IsBusy
    {
        get; set;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanToggle))]
    public partial bool IsEditable
    {
        get; set;
    }

    [ObservableProperty]
    public partial string GuidanceMessage
    {
        get; set;
    }

    [ObservableProperty]
    public partial bool IsStatusOpen
    {
        get; set;
    }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial PickerStatusSeverity StatusSeverity
    {
        get; set;
    }

    public bool CanToggle => IsEditable && !IsBusy;

    public string StateDescription => IsBusy
        ? "WifiAccess_StateChecking".GetLocalized()
        : !_isStateKnown
            ? "WifiAccess_StateUnavailable".GetLocalized()
            : IsWifiEnabled
                ? "WifiAccess_StateOn".GetLocalized()
                : "WifiAccess_StateOff".GetLocalized();

    public async Task SelectDeviceAsync(AppleDeviceInfo? device)
    {
        _selectionCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        var cancellationToken = cancellation.Token;
        _selectionCancellation = cancellation;
        _selectedDevice = device;
        _isStateKnown = false;
        OnPropertyChanged(nameof(StateDescription));
        IsStatusOpen = false;
        IsEditable = false;

        if (device is null)
        {
            IsWifiEnabled = false;
            GuidanceMessage = "WifiAccess_SelectDevice".GetLocalized();
            return;
        }

        if (!device.IsPaired)
        {
            IsWifiEnabled = false;
            GuidanceMessage = "WifiAccess_TrustRequired".GetLocalized();
            return;
        }

        if (device.ConnectionKind == AppleDeviceConnectionKind.WiFi)
        {
            _isStateKnown = true;
            IsWifiEnabled = true;
            GuidanceMessage = "WifiAccess_UsbRequired".GetLocalized();
            return;
        }

        if (device.ConnectionKind != AppleDeviceConnectionKind.Usb)
        {
            IsWifiEnabled = false;
            GuidanceMessage = "WifiAccess_UsbRequired".GetLocalized();
            return;
        }

        try
        {
            IsBusy = true;
            GuidanceMessage = "WifiAccess_Description".GetLocalized();
            IsWifiEnabled = await _deviceService.GetWifiConnectionsEnabledAsync(
                device,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _isStateKnown = true;
            OnPropertyChanged(nameof(StateDescription));
            IsEditable = true;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            ShowStatus(
                string.Format("WifiAccess_Error".GetLocalized(), exception.Message),
                PickerStatusSeverity.Error);
        }
        finally
        {
            if (ReferenceEquals(_selectionCancellation, cancellation))
            {
                IsBusy = false;
            }
        }
    }

    public async Task SetWifiEnabledAsync(bool enabled)
    {
        if (!CanToggle || _selectedDevice is not { } device || IsWifiEnabled == enabled)
        {
            return;
        }

        var previousValue = IsWifiEnabled;
        var cancellation = _selectionCancellation;
        var cancellationToken = cancellation?.Token ?? default;
        try
        {
            IsBusy = true;
            IsStatusOpen = false;
            await _deviceService.SetWifiConnectionsEnabledAsync(
                device,
                enabled,
                cancellationToken);
            var confirmedValue = await _deviceService.GetWifiConnectionsEnabledAsync(
                device,
                cancellationToken);
            if (!ReferenceEquals(_selectionCancellation, cancellation))
            {
                return;
            }

            IsWifiEnabled = confirmedValue;
            _isStateKnown = true;
            OnPropertyChanged(nameof(StateDescription));
            ShowStatus(
                (IsWifiEnabled ? "WifiAccess_Enabled" : "WifiAccess_Disabled").GetLocalized(),
                PickerStatusSeverity.Success);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            IsWifiEnabled = previousValue;
            ShowStatus(
                string.Format("WifiAccess_Error".GetLocalized(), exception.Message),
                PickerStatusSeverity.Error);
        }
        finally
        {
            if (ReferenceEquals(_selectionCancellation, cancellation))
            {
                IsBusy = false;
            }
        }
    }

    public void Dispose()
    {
        _selectionCancellation?.Cancel();
        _selectionCancellation?.Dispose();
        _selectionCancellation = null;
        GC.SuppressFinalize(this);
    }

    private void ShowStatus(string message, PickerStatusSeverity severity)
    {
        StatusMessage = message;
        StatusSeverity = severity;
        IsStatusOpen = true;
    }
}
