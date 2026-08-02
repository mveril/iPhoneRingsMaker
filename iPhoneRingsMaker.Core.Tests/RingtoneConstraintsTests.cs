using iPhoneRingsMaker.Core.Models;

namespace iPhoneRingsMaker.Core.Tests;

public sealed class RingtoneConstraintsTests
{
    [Fact]
    public void IsValidRange_WithExactlyThirtySeconds_ReturnsTrue()
    {
        var result = RingtoneConstraints.IsValidRange(
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(40),
            TimeSpan.FromMinutes(3));

        Assert.True(result);
    }

    [Theory]
    [InlineData(-1, 10, 100)]
    [InlineData(10, 10, 100)]
    [InlineData(10, 41, 100)]
    [InlineData(10, 30, 29)]
    public void IsValidRange_WithInvalidRange_ReturnsFalse(double start, double end, double mediaDuration)
    {
        var result = RingtoneConstraints.IsValidRange(
            TimeSpan.FromSeconds(start),
            TimeSpan.FromSeconds(end),
            TimeSpan.FromSeconds(mediaDuration));

        Assert.False(result);
    }

    [Fact]
    public void ValidateRange_WithValidRange_DoesNotThrow()
    {
        RingtoneConstraints.ValidateRange(
            TimeSpan.Zero,
            RingtoneConstraints.MaximumDuration,
            TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void ValidateRange_WithTooLongRange_ThrowsForEndTime()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            RingtoneConstraints.ValidateRange(
                TimeSpan.Zero,
                RingtoneConstraints.MaximumDuration + TimeSpan.FromMilliseconds(1),
                TimeSpan.FromMinutes(1)));

        Assert.Equal("endTime", exception.ParamName);
    }
}
