using CsTools.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using WinUITools.ItemsRepeaterExtensions;

namespace FileCommander.Controllers;

static class CopyTools
{
    public static IEnumerable<ConflictItem> GetConflicts(ItemBase[] source, ItemBase[] target)
    {
        var sourceFiles = source.OfType<FileItem>();
        var targetFiles = target.OfType<FileItem>();
        var targetDictionary = targetFiles.ToDictionary(n => n.Name);
        return sourceFiles.SelectFilterNull(RetrieveConflict);

        ConflictItem? RetrieveConflict(FileItem item)
        {
            if (!targetDictionary.TryGetValue(item.Name, out var target))
                return null;
            return new ConflictItem(item.Name, null, item.DateTime, target.DateTime, item.Size, target.Size, item.Version, target.Version);
        }
    }

    
}

class ConflictItem(
    string name,
    string? path, 
    DateTime sourceDate, 
    DateTime targetDate, 
    long sourceSize, 
    long targetSize,
    FileVersionInfo? sourceVersion,
    FileVersionInfo? targetVersion) : ColumnViewItem 
{
    public string Name { get => name;  }
    public string? Path { get => path; }
    public DateTime SourceDate { get => sourceDate; }
    public DateTime TargetDate { get => targetDate; }
    public long SourceSize { get => sourceSize; }
    public long TargetSize { get => targetSize; }
    public FileVersionInfo? SourceVersion { get => sourceVersion; }
    public FileVersionInfo? TargetVersion { get => targetVersion; } 
}