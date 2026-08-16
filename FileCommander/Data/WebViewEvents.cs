namespace FileCommander.Data;

record Event(
    Refresh? Refresh = null,
    Reload? Reload = null,
    ToggleSelection? ToggleSelection = null,
    ChangePath? ChangePath = null,
    ChangedItems? ChangedItems = null
);

record Refresh();
record Reload();
record ToggleSelection();
record ChangePath(string Path);
record ChangedItems(Item[] Items);


