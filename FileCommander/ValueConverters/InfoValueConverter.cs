using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using System;

namespace FileCommander.ValueConverters;

public class InfoValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool active && active == true
            ? new SolidColorBrush(Colors.LightBlue)
            : new SolidColorBrush(Colors.Transparent);

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
