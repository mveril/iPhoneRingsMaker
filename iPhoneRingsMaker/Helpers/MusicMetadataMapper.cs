using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iPhoneRingsMaker.Core.Models;
using Riok.Mapperly.Abstractions;

namespace iPhoneRingsMaker.Helpers;

[Mapper]
public partial class MusicMetadataMapper
{
    [MapperIgnoreSource(nameof(Windows.Storage.FileProperties.MusicProperties.Conductors))]
    [MapperIgnoreSource(nameof(Windows.Storage.FileProperties.MusicProperties.Producers))]
    [MapperIgnoreSource(nameof(Windows.Storage.FileProperties.MusicProperties.Publisher))]
    [MapperIgnoreSource(nameof(Windows.Storage.FileProperties.MusicProperties.Rating))]
    [MapperIgnoreSource(nameof(Windows.Storage.FileProperties.MusicProperties.Subtitle))]
    [MapperIgnoreSource(nameof(Windows.Storage.FileProperties.MusicProperties.Writers))]
    public partial MusicMetadata MapMusicProperties(Windows.Storage.FileProperties.MusicProperties musicProps);
}
