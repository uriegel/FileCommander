using CsTools.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using WinUITools.ItemsRepeaterExtensions;

namespace FileCommander.Controllers;

static class CopyTools
{
    public static IEnumerable<ConflictItem> GetConflicts(ItemBase[] source, ItemBase[] target, string path)
    {
        var sourceDirs = source.OfType<DirectoryItem>();
        var targetDirs = target.OfType<DirectoryItem>();
        var sourceFiles = source.OfType<FileItem>();
        var targetFiles = target.OfType<FileItem>();
        var targetDirsDictionary = targetDirs.ToDictionary(n => n.Name);
        var targetFilesDictionary = targetFiles.ToDictionary(n => n.Name);
        var conflictDirs = sourceDirs.SelectFilterNull(RetrieveDirConflict);
        var conflictFiles = sourceFiles.SelectFilterNull(RetrieveFileConflict);
        return [.. conflictDirs, .. conflictFiles];

        ConflictItem? RetrieveDirConflict(DirectoryItem item)
        {
            if (!targetDirsDictionary.TryGetValue(item.Name, out var target))
                return null;
            var iconIndex = GetIconIndex(item.Name, path);
            return new ConflictItem(item.Name, iconIndex, item.DateTime, target.DateTime, default, default, null, null, true);
        }

        ConflictItem? RetrieveFileConflict(FileItem item)
        {
            if (!targetFilesDictionary.TryGetValue(item.Name, out var target))
                return null;
            var iconIndex = GetIconIndex(item.Name, path);
            return new ConflictItem(item.Name, iconIndex, item.DateTime, target.DateTime, item.Size, target.Size, item.Version, target.Version, false);
        }
    }

    static string? GetIconIndex(string name, string path)
    {
        var ext = name.GetFileExtension();
        return ext?.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase) == true
            ? path.AppendPath(name)
            : ext;
    }
}

class ConflictItem(
    string name,
    string? iconIndex, 
    DateTime sourceDate, 
    DateTime targetDate, 
    long? sourceSize, 
    long? targetSize,
    FileVersionInfo? sourceVersion,
    FileVersionInfo? targetVersion,
    bool isDirectory) : ColumnViewItem 
{
    public string Name { get => name;  }

    public bool IsDirectory {  get => isDirectory; }
    public string? IconIndex { get => iconIndex; }
    public DateTime SourceDate { get => sourceDate; }
    public DateTime TargetDate { get => targetDate; }
    public long? SourceSize { get => sourceSize; }
    public long? TargetSize { get => targetSize; }
    public FileVersionInfo? SourceVersion { get => sourceVersion; }
    public FileVersionInfo? TargetVersion { get => targetVersion; } 
}