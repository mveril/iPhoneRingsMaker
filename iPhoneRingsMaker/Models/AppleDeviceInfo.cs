namespace iPhoneRingsMaker.Models;

public enum AppleDeviceConnectionKind
{
    Usb,
    WiFi,
    Unknown,
}

public sealed record AppleOperatingSystemVersion(string DisplayName, Version Version)
{
    public static AppleOperatingSystemVersion? Create(Version? version, string? deviceClass)
    {
        if (version is null)
        {
            return null;
        }

        var displayName = deviceClass?.Trim().ToUpperInvariant() switch
        {
            "IPHONE" or "IPOD" => version.Major < 4 ? "iPhoneOS" : "iOS",
            "IPAD" => version.Major >= 13 ? "iPadOS" : "iOS",
            "WATCH" or "APPLEWATCH" => "watchOS",
            "APPLETv" or "APPLETV" => version.Major >= 9 ? "tvOS" : "iOS",
            _ => "iOS",
        };

        return new AppleOperatingSystemVersion(displayName, version);
    }

    public override string ToString()
    {
        return $"{DisplayName} {Version}";
    }
}

public sealed record AppleDeviceInfo(
    string Identifier,
    string Name,
    Version? IOSVersion,
    string? DeviceClass,
    string? ProductType,
    AppleDeviceConnectionKind ConnectionKind,
    bool IsPaired)
{
    public string ConnectionDescription => ConnectionKind switch
    {
        AppleDeviceConnectionKind.Usb => "USB",
        AppleDeviceConnectionKind.WiFi => "Wi-Fi",
        _ => "—",
    };

    public AppleOperatingSystemVersion? OperatingSystem =>
        AppleOperatingSystemVersion.Create(IOSVersion, DeviceClass);

    public string IOSVersionDescription => OperatingSystem?.ToString() ?? "iOS version unavailable";

    public string Subtitle => $"{ConnectionDescription} · {IOSVersionDescription}";
}
