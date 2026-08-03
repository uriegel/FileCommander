using System.ComponentModel;
using System.Windows.Input;

namespace FileCommander.Contexts;

class MainContext : INotifyPropertyChanged
{
    public static MainContext Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ShowHiddenCommand { get; set; } = null!;
    public ICommand RefreshCommand { get; set; } = null!;

    public bool ShowHidden
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(ShowHidden));
            }
        }
    }

    void OnChanged(string name) => PropertyChanged?.Invoke(this, new(name));
}
