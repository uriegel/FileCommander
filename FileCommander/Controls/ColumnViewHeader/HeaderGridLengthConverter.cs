using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

using System;

namespace FileCommander.Controls.ColumnViewHeader;


public class HeaderGridLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => new GridLength((double)value, GridUnitType.Star);

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => new GridLength((double)value, GridUnitType.Star);
}