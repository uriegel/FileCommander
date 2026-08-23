using FileCommander.Data;

using Microsoft.UI.Xaml.Data;

using System;

namespace FileCommander.ValueConverters;

public class SizeConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
        => (value is long item) ? item.FormatSize() : null;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
