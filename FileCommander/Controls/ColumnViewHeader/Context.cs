using System.ComponentModel;

namespace FileCommander.Controls.ColumnViewHeader;

public class Context : INotifyPropertyChanged
{
    public double[] StarLength
    {
        get;
        set
        {
            field = value;
            OnChanged(nameof(StarLength));
        }
    } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
