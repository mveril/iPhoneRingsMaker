using System.Buffers.Binary;

namespace iPhoneRingsMaker.Core.Models;

public static class M4rDurationReader
{
    public static TimeSpan Read(ReadOnlySpan<byte> contents)
    {
        var duration = FindMovieHeaderDuration(contents);
        if (duration is null or <= 0 || double.IsInfinity(duration.Value) || double.IsNaN(duration.Value))
        {
            throw new InvalidDataException("The M4R duration could not be read.");
        }

        return TimeSpan.FromSeconds(duration.Value);
    }

    private static double? FindMovieHeaderDuration(ReadOnlySpan<byte> contents)
    {
        var offset = 0;
        while (offset + 8 <= contents.Length)
        {
            var atomSize = BinaryPrimitives.ReadUInt32BigEndian(contents[offset..]);
            var headerSize = 8;
            long size = atomSize;
            if (atomSize == 1)
            {
                if (offset + 16 > contents.Length)
                {
                    return null;
                }

                var extendedSize = BinaryPrimitives.ReadUInt64BigEndian(contents[(offset + 8)..]);
                if (extendedSize > int.MaxValue)
                {
                    return null;
                }

                size = (long)extendedSize;
                headerSize = 16;
            }
            else if (atomSize == 0)
            {
                size = contents.Length - offset;
            }

            if (size < headerSize || size > int.MaxValue || offset + size > contents.Length)
            {
                return null;
            }

            var type = contents.Slice(offset + 4, 4);
            var payload = contents.Slice(offset + headerSize, (int)size - headerSize);
            if (type.SequenceEqual("mvhd"u8))
            {
                return ReadMovieHeader(payload);
            }

            if (type.SequenceEqual("moov"u8))
            {
                var nested = FindMovieHeaderDuration(payload);
                if (nested is not null)
                {
                    return nested;
                }
            }

            offset += (int)size;
        }

        return null;
    }

    private static double? ReadMovieHeader(ReadOnlySpan<byte> payload)
    {
        if (payload.IsEmpty)
        {
            return null;
        }

        var version = payload[0];
        var timeScaleOffset = version == 1 ? 20 : 12;
        var durationOffset = version == 1 ? 24 : 16;
        var durationSize = version == 1 ? sizeof(ulong) : sizeof(uint);
        if (version > 1 || payload.Length < durationOffset + durationSize)
        {
            return null;
        }

        var timeScale = BinaryPrimitives.ReadUInt32BigEndian(payload[timeScaleOffset..]);
        var duration = version == 1
            ? BinaryPrimitives.ReadUInt64BigEndian(payload[durationOffset..])
            : BinaryPrimitives.ReadUInt32BigEndian(payload[durationOffset..]);
        return timeScale == 0 ? null : (double)duration / timeScale;
    }
}
