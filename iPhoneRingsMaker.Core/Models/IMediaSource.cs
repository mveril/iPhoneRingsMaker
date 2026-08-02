using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace iPhoneRingsMaker.Core.Models;

[JsonDerivedType(typeof(LocalMediaSource), typeDiscriminator: "local")]
[JsonDerivedType(typeof(IPhoneMediaSource), typeDiscriminator: "iphone")]
public interface IMediaSource : IEquatable<IMediaSource>
{

}
