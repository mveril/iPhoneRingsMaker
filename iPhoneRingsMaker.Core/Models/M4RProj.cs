using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Generator.Equals;

namespace iPhoneRingsMaker.Core.Models;

[Equatable]
public partial class M4RProj : INotifyPropertyChanged, IEquatable<M4RProj>
{
    private IMediaSource _mediaSource;
    public required IMediaSource MediaSource
    {
        get => _mediaSource;
        set
        {
            if (_mediaSource != value)
            {
                _mediaSource = value;
                OnPropertyChanged();
            }
        }
    }

    private TimeSpan _startTime;
    public TimeSpan StartTime
    {
        get => _startTime;
        set
        {
            if (_startTime != value)
            {
                _startTime = value;
                OnPropertyChanged();
            }
        }
    }

    private TimeSpan? _endTime;

    public TimeSpan? EndTime
    {
        get => _endTime;
        set
        {
            if (_endTime != value)
            {
                _endTime = value;
                OnPropertyChanged();
            }
        }
    }

    public void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, e);
    }

    public void OnPropertyChanged([CallerMemberName] string member = "")
    {
        OnPropertyChanged(new PropertyChangedEventArgs(member));
    }

    public event PropertyChangedEventHandler PropertyChanged;
}
