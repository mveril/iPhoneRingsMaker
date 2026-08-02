using iPhoneRingsMaker.Models;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace iPhoneRingsMaker.Converters;

public sealed class PickerStatusSeverityToInfoBarSeverityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is PickerStatusSeverity severity
            ? severity switch
            {
                PickerStatusSeverity.Success => InfoBarSeverity.Success,
                PickerStatusSeverity.Warning => InfoBarSeverity.Warning,
                PickerStatusSeverity.Error => InfoBarSeverity.Error,
                _ => InfoBarSeverity.Informational,
            }
            : InfoBarSeverity.Informational;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
