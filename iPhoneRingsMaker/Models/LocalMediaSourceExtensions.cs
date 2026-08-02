using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iPhoneRingsMaker.Core.Models;
using Windows.Storage;

namespace iPhoneRingsMaker.Models;

internal static class LocalMediaSourceExtensions
{
    public static async Task<StorageFile> GetStorageFileAsync(this LocalMediaSource localMediaSource)
    {
        return await StorageFile.GetFileFromPathAsync(localMediaSource.Path);
    }

    public static FileInfo GetFileInfo(this LocalMediaSource localMediaSource)
    {
        return new FileInfo(localMediaSource.Path);
    }
}
