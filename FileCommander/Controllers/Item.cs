using CsTools;
using CsTools.Extensions;

using System;
using System.Diagnostics;
using System.IO;

namespace FileCommander.Controllers;

abstract record ItemBase(string Name, bool IsHidden)
{
    public abstract string GetIcon(string path);
}
record ParentItem() : ItemBase("..", false)
{
    public override string GetIcon(string path) => "iconFromRes/GoUp";
}
record DirectoryItem(string Name, bool IsHidden) : ItemBase(Name, IsHidden)
{
    public DateTime DateTime { get; set; }
    public static DirectoryItem Create(DirectoryInfo info) => new(info.Name, info.IsHidden()) { DateTime = info.LastWriteTime };
    public override string GetIcon(string path) => "iconFromRes/Folder";
}
record FileItem(string Name, bool IsVisible) : ItemBase(Name, IsVisible)
{
    public long Size { get; set; }
    public DateTime DateTime { get; set; }
    public ExifData? ExifData { get; set; }
    public FileVersionInfo? Version { get; set; }
    public static FileItem Create(FileInfo info) => new(info.Name, info.IsHidden())
    { 
        Size = info.Length,
        DateTime = info.LastWriteTime
    };
    public override string GetIcon(string path)
        => $"icon/{(Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? path.AppendPath(Name) : Name.GetFileExtension())}";
}

static class ItemExtensions
{
    public static bool IsHidden(this FileSystemInfo info)
        => (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden || info.Name.StartsWith('.');
}
