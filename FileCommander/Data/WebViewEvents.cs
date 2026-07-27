namespace FileCommander.Data;

record Event(
    GetItems? GetItems = null
);

record GetItems(Column[]? Columns = null);

record Column(string Name);