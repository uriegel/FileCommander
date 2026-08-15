namespace FileCommander.Data;

record ChangesResult(Change[]? Changes);
record Change(Item? Item = null, DeleteChange? Deleted = null);
record DeleteChange(int Position, int Selection);