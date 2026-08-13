using CsTools.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FileCommander.Controllers;

class ItemsComparer(int sortIndex, bool subColumn, bool descending) : IComparer<ItemBase>
{
    public int Compare(ItemBase? x, ItemBase? y)
    {
        // Parent item always first
        int tx = x switch
        {
            ParentItem => 0,
            DirectoryItem => 1,
            _ => 2
        };

        int ty = y switch
        {
            ParentItem => 0,
            DirectoryItem => 1,
            _ => 2
        };

        int result = tx.CompareTo(ty);
        if (result != 0)
            return result;

        result = (sortIndex, subColumn) switch
        {
            (0, false) => StringComparer.CurrentCultureIgnoreCase.Compare(x!.Name, y!.Name),
            (0, true) => StringComparer.CurrentCultureIgnoreCase.Compare(x!.Name.GetFileExtension(), y!.Name.GetFileExtension()),
            (1, _) => CompareDate(x!, y!),
            (2, _) => CompareSize(x!, y!),
            (3, _) => CompareVersion(x!, y!),
            _ => 0
        };

        return descending ? -result : result;
    }

    static int CompareDate(ItemBase x, ItemBase y)
    {
        DateTime dx = x switch
        {
            DirectoryItem d => d.DateTime,
            FileItem f => f.DateTime,
            _ => DateTime.MinValue
        };

        DateTime dy = y switch
        {
            DirectoryItem d => d.DateTime,
            FileItem f => f.DateTime,
            _ => DateTime.MinValue
        };

        return dx.CompareTo(dy);
    }

    static int CompareSize(ItemBase x, ItemBase y)
    {
        long sx = x is FileItem f1 ? f1.Size : -1;
        long sy = y is FileItem f2 ? f2.Size : -1;

        return sx.CompareTo(sy);
    }

    static int CompareVersion(ItemBase x, ItemBase y)
    {
        var sx = x is FileItem f1 ? f1.Version : null;
        var sy = y is FileItem f2 ? f2.Version : null;
        return (sx != null && sy != null)
            ? CompareVersion(sx, sy)
            : sx == null && sy == null
            ? 0
            : sy == null
            ? 1
            : -1;
    }

    static int CompareVersion(FileVersionInfo x, FileVersionInfo y)
        => x.FileMajorPart != y.FileMajorPart
            ? x.FileMajorPart - y.FileMajorPart
            : x.FileMinorPart != y.FileMinorPart
            ? x.FileMinorPart - y.FileMinorPart
            : x.FileBuildPart != y.FileBuildPart
            ? x.FileBuildPart - y.FileBuildPart
            : x.FilePrivatePart - y.FilePrivatePart;
}