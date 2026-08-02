using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace iPhoneRingsMaker.Contracts.Models;

internal interface IMedia<IMediaSource> : IMedia
{
    public IMediaSource MediaSource
    {
        get;
    }
}
