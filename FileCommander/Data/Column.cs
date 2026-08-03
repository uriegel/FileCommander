using System.Data.Common;

namespace FileCommander.Data;

record Column(string Text, bool? IsRightAligned = null, string? SubColumn = null, bool? Sortable = null);
