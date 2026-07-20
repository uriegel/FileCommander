using Microsoft.UI.Xaml.Data;

using System;
using System.Collections.Generic;
using System.Text;

namespace FileCommander.ValueConverter;

public class DateTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value != null && value is DateTime time ? time.ToString("g") : "";
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
