using FileCommander.Extensions;

using Microsoft.UI.Xaml.Data;

using System;

namespace FileCommander.ValueConverter;

public class IconImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
        => ShellIconCache.GetIcon(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, string language) 
        => throw new NotImplementedException();
}
