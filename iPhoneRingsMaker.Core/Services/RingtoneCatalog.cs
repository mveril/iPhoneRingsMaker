using System.Buffers.Binary;
using System.Security.Cryptography;

using Netimobiledevice.Plist;

namespace iPhoneRingsMaker.Core.Services;

public static class RingtoneCatalog
{
    public static byte[] AddRingtone(
        byte[]? existingContents,
        string ringtoneFileName,
        string title,
        TimeSpan duration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ringtoneFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        DictionaryNode root;
        if (existingContents is { Length: > 0 })
        {
            root = PropertyList.LoadFromByteArray(existingContents) as DictionaryNode
                ?? throw new InvalidDataException("Ringtones.plist does not contain a dictionary.");
        }
        else
        {
            root = new DictionaryNode();
        }

        if (!root.TryGetValue("Ringtones", out var ringtonesNode))
        {
            ringtonesNode = new DictionaryNode();
            root["Ringtones"] = ringtonesNode;
        }

        if (ringtonesNode is not DictionaryNode ringtones)
        {
            throw new InvalidDataException("The Ringtones entry is not a dictionary.");
        }

        if (ringtones.ContainsKey(ringtoneFileName))
        {
            throw new InvalidOperationException(
                $"A ringtone named '{ringtoneFileName}' already exists.");
        }

        Span<byte> identifierBytes = stackalloc byte[sizeof(ulong)];
        RandomNumberGenerator.Fill(identifierBytes);
        var identifier = BinaryPrimitives.ReadUInt64LittleEndian(identifierBytes);
        var durationMilliseconds = checked((long)Math.Round(
            duration.TotalMilliseconds,
            MidpointRounding.AwayFromZero));
        ringtones[ringtoneFileName] = new DictionaryNode
        {
            ["Name"] = new StringNode(title),
            ["GUID"] = new StringNode(Convert.ToHexString(identifierBytes)),
            ["Total Time"] = new IntegerNode(durationMilliseconds),
            ["PID"] = new IntegerNode(identifier),
            ["Protected Content"] = new BooleanNode(false),
        };

        return PropertyList.SaveAsByteArray(root, PlistFormat.Binary);
    }
}
