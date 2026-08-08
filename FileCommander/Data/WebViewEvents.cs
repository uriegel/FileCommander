namespace FileCommander.Data;

record Event(
    Refresh? Refresh = null,
    Reload? Reload = null,
    ChangePath? ChangePath = null,
    ExtendedItem? ExtendedItem = null
);

record Refresh();
record Reload();
record ChangePath(string Path);
record ExtendedItem(Item Item);

