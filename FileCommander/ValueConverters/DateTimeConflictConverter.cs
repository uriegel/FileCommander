using FileCommander.Controllers;

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using System;

namespace FileCommander.ValueConverters;

public class DateTimeConflictConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object parameter, string language)
        => value is ConflictItem item
            ? item.SourceDate > item.TargetDate 
            ? new SolidColorBrush(new Windows.UI.Color() {  A = 50, B = 120, G = 255, R = 0})
            : item.SourceDate < item.TargetDate
            ? new SolidColorBrush(new Windows.UI.Color() { A = 120, B = 0, G = 0, R = 255 })
            : null
            : null;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
