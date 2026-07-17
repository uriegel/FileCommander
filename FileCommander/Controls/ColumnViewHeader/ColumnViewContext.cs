using Microsoft.UI.Xaml;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace FileCommander.Controls.ColumnViewHeader;

public class ColumnViewContext : INotifyPropertyChanged
{
    public GridLength[] ColumnWidths
    {
        get => field;
        set
        {
            field = value;
            OnChanged(nameof(ColumnWidths));
        }
    } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnChanged(string name) => PropertyChanged?.Invoke(this, new(name));
}

