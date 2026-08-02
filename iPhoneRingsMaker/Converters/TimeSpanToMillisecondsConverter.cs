using Microsoft.UI.Xaml.Data;

namespace iPhoneRingsMaker.Converters;

internal class TimeSpanToMillisecondsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TimeSpan timeSpan)
        {
            return timeSpan.TotalMilliseconds;
        }

        return 0d;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is double milliseconds)
        {
            return TimeSpan.FromMilliseconds(milliseconds);
        }

        return TimeSpan.Zero;
    }
}
