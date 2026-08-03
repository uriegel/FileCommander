namespace FileCommander.Data;

record Event(
    Refresh? Refresh = null,
    Reload? Reload = null
);

record Refresh();
record Reload();

