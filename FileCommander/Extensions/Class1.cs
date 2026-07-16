using System;
using System.Runtime.InteropServices;

using ClrWinApi;

using static ClrWinApi.Api;

namespace Explorer.Controls;


// TODO to ClrWinApi

internal static class ShellIconInterop
{
    private const int SHIL_SMALL = 0x1;
    private const int SHIL_LARGE = 0x0;
    private const int SHIL_EXTRALARGE = 0x2;
    private const int SHIL_JUMBO = 0x4;

    private const int ILD_TRANSPARENT = 0x00000001;

    private static readonly Guid IID_IImageList =
        new("46EB5926-582E-4017-9FDF-E8998DAA0950");


    internal static nint GetIconHandle(string pathOrExtension, int size)
    {
        var info = new ShFileInfo();
        var result = SHGetFileInfo(pathOrExtension, FileAttributes.Normal, ref info, Marshal.SizeOf<ShFileInfo>(), 
            SHGetFileInfoConstants.SYSICONINDEX | SHGetFileInfoConstants.USEFILEATTRIBUTES| SHGetFileInfoConstants.TYPENAME);
        if (result == 0)
            return 0;


        int imageListSize = size switch
        {
            <= 16 => SHIL_SMALL,
            <= 32 => SHIL_LARGE,
            <= 48 => SHIL_EXTRALARGE,
            _ => SHIL_JUMBO
        };

        var guid = IID_IImageList;
        SHGetImageList(imageListSize, ref guid, out IImageList? imageList);
        if (imageList == null)
            return 0;

        imageList.GetIcon(info.Icon, ILD_TRANSPARENT, out IntPtr hIcon);
        return hIcon;
    }

    [DllImport("shell32.dll")]
    private static extern int SHGetImageList(int iImageList, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IImageList? ppv);

    [ComImport]
    [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IImageList
    {
        [PreserveSig]
        int Add(
            IntPtr hbmImage,
            IntPtr hbmMask,
            out int pi);

        [PreserveSig]
        int ReplaceIcon(
            int i,
            IntPtr hicon,
            out int pi);

        [PreserveSig]
        int SetOverlayImage(
            int iImage,
            int iOverlay);

        [PreserveSig]
        int Replace(
            int i,
            IntPtr hbmImage,
            IntPtr hbmMask);

        [PreserveSig]
        int AddMasked(
            IntPtr hbmImage,
            uint crMask,
            out int pi);

        [PreserveSig]
        int Draw(
            IntPtr pimldp);

        [PreserveSig]
        int Remove(
            int i);

        [PreserveSig]
        int GetIcon(
            int i,
            int flags,
            out IntPtr phicon);
    }
}