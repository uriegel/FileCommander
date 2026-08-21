using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

using System;

namespace FileCommander.ValueConverters;

public class FromRightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => (bool?)value == true ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
