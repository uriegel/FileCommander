namespace FileCommander.Data;

record Column(string Text, bool? IsRightAligned = null, string? SubColumn = null, bool? Sortable = null);

record Columns(string Id, bool AllowDrop, Column[] Values);