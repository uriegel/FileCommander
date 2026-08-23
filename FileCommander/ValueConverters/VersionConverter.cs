using FileCommander.Data;

using Microsoft.UI.Xaml.Data;

using System;
using System.Diagnostics;

namespace FileCommander.ValueConverters;

public class VersionConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
        => (value is FileVersionInfo item) ? item.Format() : null;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
