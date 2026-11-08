using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace FileCommander.DragDrop;

[ComImport]
[Guid("3D25F6D6-4B2A-433C-9184-7C33AD35D001")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDataObjectProvider
{
    [PreserveSig]
    int GetDataObject(out nint dataObject);

    [PreserveSig]
    int SetDataObject(nint dataObject);
}

[GeneratedComInterface]
[Guid("0000010E-0000-0000-C000-000000000046")]
internal partial interface IDataObject
{
    [PreserveSig]
    int GetData(nint pFormatEtc, nint pMedium);

    [PreserveSig]
    int GetDataHere(nint pFormatEtc, nint pMedium);

    [PreserveSig]
    int QueryGetData(nint pFormatEtc);

    [PreserveSig]
    int GetCanonicalFormatEtc(nint pFormatEtcIn, nint pFormatEtcOut);

    [PreserveSig]
    int SetData(nint pFormatEtc, nint pMedium, int fRelease);

    [PreserveSig]
    int EnumFormatEtc(uint dwDirection, nint ppenumFormatEtc);

    [PreserveSig]
    int DAdvise(nint pFormatEtc, uint advf, nint pAdvSink, nint pdwConnection);

    [PreserveSig]
    int DUnadvise(uint dwConnection);

    [PreserveSig]
    int EnumDAdvise(nint ppenumAdvise);
}

[StructLayout(LayoutKind.Sequential)]
internal struct FormatEtc
{
    public ushort cfFormat;
    public nint ptd;
    public uint dwAspect;
    public int lindex;
    public uint tymed;
}

[GeneratedComInterface]
[Guid("00000103-0000-0000-C000-000000000046")]
internal partial interface IEnumFORMATETC
{
    [PreserveSig]
    int Next(uint celt, nint rgelt, nint pceltFetched);

    [PreserveSig]
    int Skip(uint celt);

    [PreserveSig]
    int Reset();

    [PreserveSig]
    int Clone(out nint ppenum);
}

[StructLayout(LayoutKind.Sequential)]
internal struct StgMedium
{
    public uint tymed;
    public nint unionmember;
    public nint pUnkForRelease;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DropFiles
{
    public int pFiles;
    public int x;
    public int y;
    public int fNC;
    public int fWide;
}

[GeneratedComClass]
internal partial class ShellDataObject : IDataObject
{
    private const ushort CF_HDROP = 15;

    private const uint DVASPECT_CONTENT = 1;
    private const uint TYMED_HGLOBAL = 1;

    private const uint GMEM_MOVEABLE = 0x0002;

    private const int S_OK = 0;
    private const int E_POINTER = unchecked((int)0x80004003);
    private const int DV_E_FORMATETC = unchecked((int)0x80040064);
    private const int DV_E_DVTARGETDEVICE = unchecked((int)0x80040065);
    private const int DV_E_DVASPECT = unchecked((int)0x8004006B);
    private const int DV_E_LINDEX = unchecked((int)0x80040068);
    private const int DV_E_TYMED = unchecked((int)0x80040069);
    private const int E_OUTOFMEMORY = unchecked((int)0x8007000E);

    private readonly string[] filePaths;

    public ShellDataObject(string[] filePaths)
    {
        this.filePaths = filePaths;
    }

    public int QueryGetData(nint pFormatEtc)
    {
        if (pFormatEtc == 0)
            return E_POINTER;

        var format = Marshal.PtrToStructure<FormatEtc>(pFormatEtc);

        if (format.cfFormat != CF_HDROP)
            return DV_E_FORMATETC;

        if (format.ptd != 0)
            return DV_E_DVTARGETDEVICE;

        if (format.dwAspect != DVASPECT_CONTENT)
            return DV_E_DVASPECT;

        if (format.lindex != -1)
            return DV_E_LINDEX;

        if ((format.tymed & TYMED_HGLOBAL) == 0)
            return DV_E_TYMED;

        return S_OK;
    }

    public int GetData(nint pFormatEtc, nint pMedium)
    {
        if (pMedium == 0)
            return E_POINTER;

        int result = QueryGetData(pFormatEtc);

        if (result != S_OK)
            return result;

        Marshal.StructureToPtr(
            new StgMedium(),
            pMedium,
            false);

        nint memory = 0;

        try
        {
            // DROPFILES header followed by UTF-16 paths.
            string paths = string.Join('\0', filePaths) + "\0\0";
            byte[] pathBytes = Encoding.Unicode.GetBytes(paths);

            int headerSize = Marshal.SizeOf<DropFiles>();
            int totalSize = checked(headerSize + pathBytes.Length);

            memory = GlobalAlloc(GMEM_MOVEABLE, (nuint)totalSize);

            if (memory == 0)
                return E_OUTOFMEMORY;

            nint pointer = GlobalLock(memory);

            if (pointer == 0)
            {
                GlobalFree(memory);
                return E_OUTOFMEMORY;
            }

            try
            {
                var dropFiles = new DropFiles
                {
                    pFiles = headerSize,
                    fWide = 1
                };

                Marshal.StructureToPtr(dropFiles, pointer, false);

                Marshal.Copy(
                    pathBytes,
                    0,
                    pointer + headerSize,
                    pathBytes.Length);
            }
            finally
            {
                GlobalUnlock(memory);
            }

            // Transfer ownership of the HGLOBAL to the caller.
            Marshal.StructureToPtr(
                new StgMedium
                {
                    tymed = TYMED_HGLOBAL,
                    unionmember = memory,
                    pUnkForRelease = 0
                },
                pMedium,
                false);

            memory = 0;
            return S_OK;
        }
        catch
        {
            if (memory != 0)
                GlobalFree(memory);

            throw;
        }
    }

    public int GetDataHere(nint pFormatEtc, nint pMedium)
        => unchecked((int)0x80004001);

    public int GetCanonicalFormatEtc(nint pFormatEtcIn, nint pFormatEtcOut)
        => unchecked((int)0x80004001);

    public int SetData(nint pFormatEtc, nint pMedium, int fRelease)
        => unchecked((int)0x80004001);

    private const string CFSTR_PREFERREDDROPEFFECT =
    "Preferred DropEffect";

    private const uint DROPEFFECT_COPY = 1;
    private const uint DROPEFFECT_MOVE = 2;

    private static readonly ushort CF_PREFERREDDROPEFFECT =
        RegisterClipboardFormat(CFSTR_PREFERREDDROPEFFECT);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClipboardFormat(string lpszFormat);


    public int EnumFormatEtc(uint dwDirection, nint ppenumFormatEtc)
    {
        const uint DATADIR_GET = 1;

        if (ppenumFormatEtc == 0)
            return E_POINTER;

        Marshal.WriteIntPtr(ppenumFormatEtc, 0);

        if (dwDirection != DATADIR_GET)
            return unchecked((int)0x80004001); // E_NOTIMPL

        var format = new FormatEtc
        {
            cfFormat = CF_HDROP,
            ptd = 0,
            dwAspect = DVASPECT_CONTENT,
            lindex = -1,
            tymed = TYMED_HGLOBAL
        };

        var formats = new[]
{
    new FormatEtc
    {
        cfFormat = CF_HDROP,
        dwAspect = DVASPECT_CONTENT,
        lindex = -1,
        tymed = TYMED_HGLOBAL
    },
    new FormatEtc
    {
        cfFormat = CF_PREFERREDDROPEFFECT,
        dwAspect = DVASPECT_CONTENT,
        lindex = -1,
        tymed = TYMED_HGLOBAL
    }
};

        var enumerator = new FormatEtcEnumerator(formats);

        var wrappers = new StrategyBasedComWrappers();

        nint ptr = wrappers.GetOrCreateComInterfaceForObject(
            enumerator,
            CreateComInterfaceFlags.None);

        Marshal.WriteIntPtr(ppenumFormatEtc, ptr);

        return S_OK;
    }
    public int DAdvise(nint pFormatEtc, uint advf, nint pAdvSink, nint pdwConnection)
        => unchecked((int)0x80004001);

    public int DUnadvise(uint dwConnection)
        => unchecked((int)0x80004001);

    public int EnumDAdvise(nint ppenumAdvise)
        => unchecked((int)0x80004001);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalAlloc(uint uFlags, nuint dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalLock(nint hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(nint hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalFree(nint hMem);
}


[GeneratedComClass]
internal partial class FormatEtcEnumerator : IEnumFORMATETC
{
    private static readonly StrategyBasedComWrappers Wrappers = new();

    private readonly FormatEtc[] formats;
    private int index;

    public FormatEtcEnumerator(FormatEtc[] formats, int index = 0)
    {
        this.formats = formats;
        this.index = index;
    }

    private const int S_OK = 0;
    private const int S_FALSE = 1;
    private const int E_POINTER = unchecked((int)0x80004003);

    public int Next(uint celt, nint rgelt, nint pceltFetched)
    {
        if (rgelt == 0)
            return E_POINTER;

        if (celt != 1 && pceltFetched == 0)
            return E_POINTER;

        uint fetched = 0;
        int size = Marshal.SizeOf<FormatEtc>();

        while (fetched < celt && index < formats.Length)
        {
            nint destination = rgelt + (nint)(fetched * (uint)size);

            Marshal.StructureToPtr(
                formats[index],
                destination,
                false);

            index++;
            fetched++;
        }

        if (pceltFetched != 0)
            Marshal.WriteInt32(pceltFetched, (int)fetched);

        return fetched == celt ? S_OK : S_FALSE;
    }

    public int Skip(uint celt)
    {
        int remaining = formats.Length - index;
        int skipped = Math.Min((int)celt, remaining);

        index += skipped;

        return skipped == celt ? 0 : 1;
    }

    public int Reset()
    {
        index = 0;
        return 0;
    }

    private static readonly Guid IID_IEnumFORMATETC =
    new("00000103-0000-0000-C000-000000000046");

    public int Clone(out nint ppenum)
    {
        var clone = new FormatEtcEnumerator(formats, index);

        nint unknown = Wrappers.GetOrCreateComInterfaceForObject(
               clone,
               CreateComInterfaceFlags.None);

        int hr = Marshal.QueryInterface(
            unknown,
            IID_IEnumFORMATETC,
            out ppenum);

        Marshal.Release(unknown);

        return hr;
    }
}


//[GeneratedComClass]
//internal partial class FormatEtcEnumerator : IEnumFORMATETC
//{
//    private static readonly StrategyBasedComWrappers Wrappers = new();

//    private readonly FormatEtc format;
//    private bool alreadyReturned;

//    public FormatEtcEnumerator(FormatEtc format, bool alreadyReturned = false)
//    {
//        this.format = format;
//        this.alreadyReturned = alreadyReturned;
//    }

//    public int Next(uint celt, nint rgelt, nint pceltFetched)
//    {
//        uint fetched = 0;

//        if (!alreadyReturned && celt > 0)
//        {
//            Marshal.StructureToPtr(format, rgelt, false);
//            alreadyReturned = true;
//            fetched = 1;
//        }

//        if (pceltFetched != 0)
//            Marshal.WriteInt32(pceltFetched, (int)fetched);

//        return fetched == celt ? 0 : 1; // S_OK / S_FALSE
//    }

//    public int Skip(uint celt)
//    {
//        if (!alreadyReturned && celt > 0)
//        {
//            alreadyReturned = true;
//            return celt == 1 ? 0 : 1;
//        }

//        return 1;
//    }

//    public int Reset()
//    {
//        alreadyReturned = false;
//        return 0;
//    }

//    public int Clone(out nint ppenum)
//    {
//        var clone = new FormatEtcEnumerator(format, alreadyReturned);

//        ppenum = Wrappers.GetOrCreateComInterfaceForObject(
//            clone,
//            CreateComInterfaceFlags.None);

//        return 0;
//    }
//}