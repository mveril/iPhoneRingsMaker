namespace iPhoneRingsMaker.Core.Models;

public static class RingtoneConstraints
{
    public static readonly TimeSpan MaximumDuration = TimeSpan.FromSeconds(30);

    public static bool IsValidRange(TimeSpan startTime, TimeSpan endTime, TimeSpan mediaDuration)
    {
        return startTime >= TimeSpan.Zero
            && endTime > startTime
            && endTime <= mediaDuration
            && endTime - startTime <= MaximumDuration;
    }

    public static void ValidateRange(TimeSpan startTime, TimeSpan endTime, TimeSpan mediaDuration)
    {
        if (!IsValidRange(startTime, endTime, mediaDuration))
        {
            throw new ArgumentOutOfRangeException(
                nameof(endTime),
                $"The ringtone range must be inside the media and no longer than {MaximumDuration.TotalSeconds} seconds.");
        }
    }
}
