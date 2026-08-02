using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Helpers;
using iPhoneRingsMaker.Models;

namespace iPhoneRingsMaker.ViewModels;

public partial class DevicePickerViewModel : ObservableObject
{
    private readonly IAppleDeviceService _deviceService;
    private readonly string _ringtonePath;
    private bool _isActive;

    public DevicePickerViewModel(IAppleDeviceService deviceService, string ringtonePath)
    {
        _deviceService = deviceService;
        _ringtonePath = ringtonePath;
    }

    public ObservableCollection<AppleDeviceInfo> Devices { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanTransfer))]
    public partial AppleDeviceInfo? SelectedDevice
    {
        get; set;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanTransfer))]
    [NotifyPropertyChangedFor(nameof(HasNoDevices))]
    public partial bool IsLoading
    {
        get; set;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoDevices))]
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

    public bool CanTransfer => !IsLoading && SelectedDevice is not null;

    public bool HasNoDevices => !IsLoading && !IsStatusOpen && Devices.Count == 0;

    public async Task ActivateAsync()
    {
        if (_isActive)
        {
            return;
        }

        _isActive = true;
        _deviceService.DevicesChanged += OnDevicesChanged;
        _deviceService.StartWatching();
        await RefreshDevicesAsync();
    }

    public void Deactivate()
    {
        if (!_isActive)
        {
            return;
        }

        _isActive = false;
        _deviceService.DevicesChanged -= OnDevicesChanged;
        _deviceService.StopWatching();
    }

    public async Task<bool> TransferAsync()
    {
        if (SelectedDevice is not { } device)
        {
            return false;
        }

        try
        {
            IsLoading = true;
            if (!device.IsPaired)
            {
                var pairingProgress = new Progress<string>(_ => ShowStatus(
                    "DevicePicker_Pairing".GetLocalized(),
                    PickerStatusSeverity.Informational));
                device = await _deviceService.PairAsync(device, pairingProgress);
            }

            var transferProgress = new Progress<double>(progress => ShowStatus(
                string.Format("DevicePicker_TransferProgress".GetLocalized(), progress),
                PickerStatusSeverity.Informational));
            var result = await _deviceService.InstallRingtoneAsync(device, _ringtonePath, transferProgress);

            ShowStatus(
                result.Message,
                result.Status == RingtoneTransferStatus.Transferred
                    ? PickerStatusSeverity.Success
                    : PickerStatusSeverity.Warning);
            return result.Status == RingtoneTransferStatus.Transferred;
        }
        catch (Exception exception)
        {
            exception.RethrowIfDebuggerAttached();
            ShowStatus(exception.Message, PickerStatusSeverity.Error);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void OnDevicesChanged(object? sender, EventArgs e) => await RefreshDevicesAsync();

    private async Task RefreshDevicesAsync()
    {
        try
        {
            IsLoading = true;
            var selectedIdentifier = SelectedDevice?.Identifier;
            var devices = await _deviceService.GetDevicesAsync();
            Devices.Clear();
            foreach (var device in devices)
            {
                Devices.Add(device);
            }

            OnPropertyChanged(nameof(HasNoDevices));

            SelectedDevice = Devices.FirstOrDefault(device => device.Identifier == selectedIdentifier)
                ?? Devices.FirstOrDefault();
            if (Devices.Count == 0)
            {
                IsStatusOpen = false;
            }
        }
        catch (Exception exception)
        {
            exception.RethrowIfDebuggerAttached();
            ShowStatus(exception.Message, PickerStatusSeverity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ShowStatus(string message, PickerStatusSeverity severity)
    {
        StatusMessage = message;
        StatusSeverity = severity;
        IsStatusOpen = true;
    }
}
