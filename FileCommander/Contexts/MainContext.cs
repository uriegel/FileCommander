using System.ComponentModel;
using System.Windows.Input;

namespace FileCommander.Contexts;

class MainContext : INotifyPropertyChanged
{
    public static MainContext Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ShowHiddenCommand { get; set; } = null!;
    public ICommand RefreshCommand { get; set; } = null!;
    public ICommand ToggleSelectionCommand { get; set; } = null!;
    public ICommand SelectAllAboveCommand { get; set; } = null!;
    public ICommand SelectAllBeneathCommand { get; set; } = null!;
    public ICommand SelectAllCommand { get; set; } = null!;
    public ICommand SelectNoneCommand { get; set; } = null!;
    public ICommand CreateFolderCommand { get; set; } = null!;

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

    public string StatusBarText
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(StatusBarText));
            }
        }
    } = string.Empty;

    public bool StatusBarTextActive
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(StatusBarTextActive));
            }
        }
    }

    public int CurrentFileCount
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(CurrentFileCount));
            }
        }
    }

    public int CurrentDirectoryCount
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(CurrentDirectoryCount));
            }
        }
    }

    public void ChangeFolderContext(FolderContext? folderContext)
    {
        if (folderContext != null)
        {
            this.folderContext?.PropertyChanged -= FolderContextPropertyChanged;
            this.folderContext = folderContext;
            this.folderContext.PropertyChanged += FolderContextPropertyChanged;
            CurrentDirectoryCount = folderContext.CurrentDirectoryCount;
            CurrentFileCount = folderContext.CurrentFileCount;
            StatusBarText = folderContext.StatusBarText;
            StatusBarTextActive = folderContext.InfoText != null;
            //ExifData = folderContext.ExifData;
            //BackgroundAction = folderContext.BackgroundAction;
            // SelectedFiles = folderContext.SelectedFiles;
        }
    }

    void FolderContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (folderContext != null)
            switch (e.PropertyName)
            {
                case nameof(CurrentDirectoryCount):
                    CurrentDirectoryCount = folderContext.CurrentDirectoryCount;
                    break;
                case nameof(CurrentFileCount):
                    CurrentFileCount = folderContext.CurrentFileCount;
                    break;
                case nameof(StatusBarText):
                    StatusBarText = folderContext.StatusBarText;
                    StatusBarTextActive = folderContext.InfoText != null;
                    break;
                case nameof(FolderContext.SelectedPath):
                    StatusBarTextActive = folderContext.InfoText != null;
                    break;
                    //case nameof(ExifData):
                    //    ExifData = folderContext.ExifData;
                    //    break;
                    // case nameof(SelectedFiles):
                    //     SelectedFiles = folderContext.SelectedFiles;
                    //     break;
                    //case nameof(BackgroundAction):
                    //    BackgroundAction = folderContext.BackgroundAction;
                    //    break;
            }
    }

    FolderContext? folderContext;

    void OnChanged(string name) => PropertyChanged?.Invoke(this, new(name));
}
