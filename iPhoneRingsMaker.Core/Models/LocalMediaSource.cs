using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iPhoneRingsMaker.Core.Models;

[Generator.Equals.Equatable()]
public partial class LocalMediaSource : IMediaSource
{
    public required string Path
    {
        get; set;
    }

    public static readonly string[] SupportedFileTypes = [".m4a", ".mp3", ".wav", ".aac", ".wma", ".flac"];

    public bool Equals(IMediaSource other)
    {
        if (other is LocalMediaSource localMediaSource)
        {
            return Equals(localMediaSource);
        }
        return false;
    }
}