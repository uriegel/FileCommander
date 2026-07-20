using FileCommander.Controls;

using Microsoft.UI.Xaml.Data;

using System;
using System.Collections.Generic;
using System.Text;

namespace FileCommander.ValueConverter;

public class IsSelectedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value != null && (bool)value == true ? ItemGrid.Rot : ItemGrid.Durchsichtig;
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
