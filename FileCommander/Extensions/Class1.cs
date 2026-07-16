using System;
using System.Runtime.InteropServices;

namespace Explorer.Controls;

internal static class ShellIconInterop
{
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
    private const uint SHGFI_SYSICONINDEX = 0x000004000;
    private const uint SHGFI_TYPENAME = 0x000000400;

    private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

    private const int SHIL_SMALL = 0x1;
    private const int SHIL_LARGE = 0x0;
    private const int SHIL_EXTRALARGE = 0x2;
    private const int SHIL_JUMBO = 0x4;

    private const int ILD_TRANSPARENT = 0x00000001;

    private static readonly Guid IID_IImageList =
        new("46EB5926-582E-4017-9FDF-E8998DAA0950");


    internal static IntPtr GetIconHandle(string pathOrExtension, int size)
    {
        var info = new SHFILEINFO();

        IntPtr result = SHGetFileInfo(
            pathOrExtension,
            FILE_ATTRIBUTE_NORMAL,
            ref info,
            Marshal.SizeOf<SHFILEINFO>(),
            SHGFI_SYSICONINDEX |
            SHGFI_USEFILEATTRIBUTES |
            SHGFI_TYPENAME);

        if (result == IntPtr.Zero)
            return IntPtr.Zero;


        int imageListSize = size switch
        {
            <= 16 => SHIL_SMALL,
            <= 32 => SHIL_LARGE,
            <= 48 => SHIL_EXTRALARGE,
            _ => SHIL_JUMBO
        };

        var guid = IID_IImageList;
        SHGetImageList(
            imageListSize,
            ref guid,
            out IImageList? imageList);


        if (imageList == null)
            return IntPtr.Zero;


        imageList.GetIcon(
            info.iIcon,
            ILD_TRANSPARENT,
            out IntPtr hIcon);


        return hIcon;
    }

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool DestroyIcon(IntPtr hIcon);


    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        int cbFileInfo,
        uint uFlags);


    [DllImport("shell32.dll")]
    private static extern int SHGetImageList(
        int iImageList,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)]
        out IImageList? ppv);


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;

        public int iIcon;

        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }


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