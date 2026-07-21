using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

using WinUITools.Data;

namespace FileCommander.Data;

public class Item : ColumnViewItem, INotifyPropertyChanged
{
    public string Name
    {
        get;
        set
        {
            field = value;
            OnChanged(nameof(Name));
        }
    } = "";

    public bool IsSelected
    {
        get;
        set
        {
            field = value;
            OnChanged(nameof(IsSelected));
        }
    } 
    
    public bool IsHidden { get; init; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnChanged(string name) => PropertyChanged?.Invoke(this, new(name));

    protected const string HiddenNamestart = ".";
}

