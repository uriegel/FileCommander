using FileCommander.Controllers;

using Microsoft.UI.Xaml.Data;

using System;

namespace FileCommander.ValueConverters;

public class SizeConflictConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is ConflictItem item
            ? item.SourceSize != item.TargetSize
            ? 1.0
            : 0.4
            : 0.4   ;
    
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
