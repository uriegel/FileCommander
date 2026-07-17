using Microsoft.UI.Xaml;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace FileCommander.Controls.ColumnViewHeader;

public record HeaderItem : INotifyPropertyChanged
{
    public string Name { get; set; } = "";
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;
    public int Index { get; set; }

    public SortType SortType
    {
        get;
        set
        {
            field = value;
            OnChanged(nameof(SortType));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

};
