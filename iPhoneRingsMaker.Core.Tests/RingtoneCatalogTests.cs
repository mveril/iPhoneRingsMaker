using iPhoneRingsMaker.Core.Services;

using Netimobiledevice.Plist;

namespace iPhoneRingsMaker.Core.Tests;

public sealed class RingtoneCatalogTests
{
    [Fact]
    public void AddRingtone_StoresDurationAsMillisecondsWithoutMediaKind()
    {
        var contents = RingtoneCatalog.AddRingtone(
            existingContents: null,
            ringtoneFileName: "espresso.m4r",
            title: "Espresso",
            duration: TimeSpan.FromSeconds(8.1493125));

        var root = Assert.IsType<DictionaryNode>(PropertyList.LoadFromByteArray(contents));
        var ringtones = Assert.IsType<DictionaryNode>(root["Ringtones"]);
        var ringtone = Assert.IsType<DictionaryNode>(ringtones["espresso.m4r"]);
        var duration = Assert.IsType<IntegerNode>(ringtone["Total Time"]);

        Assert.Equal(8149UL, duration.Value);
        Assert.False(ringtone.ContainsKey("Media Kind"));
    }
}
