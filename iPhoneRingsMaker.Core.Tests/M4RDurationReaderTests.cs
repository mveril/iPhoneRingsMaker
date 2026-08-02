using System.Buffers.Binary;
using System.Text;

using iPhoneRingsMaker.Core.Models;

namespace iPhoneRingsMaker.Core.Tests;

public sealed class M4RDurationReaderTests
{
    [Fact]
    public void Read_WithVersionZeroMovieHeader_ReturnsDuration()
    {
        var contents = Atom("moov", MovieHeader(version: 0, timeScale: 1_000, duration: 30_500));

        var result = M4RDurationReader.Read(contents);

        Assert.Equal(TimeSpan.FromMilliseconds(30_500), result);
    }

    [Fact]
    public void Read_WithVersionOneMovieHeader_ReturnsDuration()
    {
        var contents = Atom("moov", MovieHeader(version: 1, timeScale: 48_000, duration: 1_440_000));

        var result = M4RDurationReader.Read(contents);

        Assert.Equal(TimeSpan.FromSeconds(30), result);
    }

    [Fact]
    public void Read_WithAtomsBeforeMovieHeader_FindsNestedHeader()
    {
        var free = Atom("free", [1, 2, 3, 4]);
        var moov = Atom("moov", [.. Atom("trak", []), .. MovieHeader(0, 10, 125)]);

        var result = M4RDurationReader.Read([.. free, .. moov]);

        Assert.Equal(TimeSpan.FromSeconds(12.5), result);
    }

    [Theory]
    [MemberData(nameof(InvalidFiles))]
    public void Read_WithInvalidFile_ThrowsInvalidDataException(byte[] contents)
    {
        Assert.Throws<InvalidDataException>(() => M4RDurationReader.Read(contents));
    }

    public static TheoryData<byte[]> InvalidFiles => new()
    {
        Array.Empty<byte>(),
        Atom("free", [1, 2, 3, 4]),
        Atom("moov", MovieHeader(0, 0, 30)),
        Atom("moov", MovieHeader(2, 1_000, 30_000)),
        new byte[] { 0, 0, 0, 64, (byte)'m', (byte)'o', (byte)'o', (byte)'v' },
    };

    private static byte[] MovieHeader(byte version, uint timeScale, ulong duration)
    {
        var payload = new byte[version == 1 ? 32 : 20];
        payload[0] = version;
        var timeScaleOffset = version == 1 ? 20 : 12;
        var durationOffset = version == 1 ? 24 : 16;
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(timeScaleOffset), timeScale);
        if (version == 1)
        {
            BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(durationOffset), duration);
        }
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(durationOffset), checked((uint)duration));
        }

        return Atom("mvhd", payload);
    }

    private static byte[] Atom(string type, byte[] payload)
    {
        var result = new byte[8 + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(result, (uint)result.Length);
        Encoding.ASCII.GetBytes(type).CopyTo(result, 4);
        payload.CopyTo(result, 8);
        return result;
    }
}
