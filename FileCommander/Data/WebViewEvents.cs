namespace FileCommander.Data;

record Event(
    Refresh? Refresh = null,
    Reload? Reload = null,
    ToggleSelection? ToggleSelection = null,
    SelectAllAbove? SelectAllAbove = null,
    SelectAllBeneath? SelectAllBeneath = null,
    SelectAll? SelectAll = null,
    SelectNone? SelectNone = null,
    ChangePath? ChangePath = null,
    ChangedItems? ChangedItems = null,
    DeleteItems? DeleteItems = null,
    Rename? Rename = null,
    RenameAsCopy? RenameAsCopy = null,
    Copy? Copy = null,
    Move? Move = null
);

record Refresh();
record Reload();
record ToggleSelection();
record SelectAllAbove();
record SelectAllBeneath();
record SelectAll();
record SelectNone();
record ChangePath(string Path);
record ChangedItems(Item[] Items);
record DeleteItems();
record Rename();
record RenameAsCopy();
record Copy();
record Move();