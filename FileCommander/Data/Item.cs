using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace FileCommander.Data;

public record Item : INotifyPropertyChanged
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

    public bool IsHidden { get; init; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnChanged(string name) => PropertyChanged?.Invoke(this, new(name));
}

