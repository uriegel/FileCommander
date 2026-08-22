using CsTools.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

record ConflictItem(
    string Name, 
    string? Path, 
    DateTime SourceDate, 
    DateTime TargetDate, 
    long SourceSize, 
    long TargetSize,
    FileVersionInfo? SourceVersion,
    FileVersionInfo? TargetVersion);