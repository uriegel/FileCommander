namespace FileCommander.Data;

record Event(
    ColumnsChanged? ColumnsChanged = null
);

record ColumnsChanged(Column[]? Columns = null);

record Column(string Name);