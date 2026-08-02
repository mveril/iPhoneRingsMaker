namespace iPhoneRingsMaker.Models;

public enum RingtoneTransferStatus
{
    Transferred,
    ManualFallbackRequired,
    Cancelled,
    Failed,
}

public sealed record RingtoneTransferResult(
    RingtoneTransferStatus Status,
    string Message)
{
    public bool Succeeded => Status == RingtoneTransferStatus.Transferred;
}
