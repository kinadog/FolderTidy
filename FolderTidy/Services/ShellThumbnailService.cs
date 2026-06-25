namespace FolderTidy.Services;

using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FolderTidy.Models;

public static class ShellThumbnailService
{
    [Flags]
    private enum SIIGBF
    {
        ResizeToFit = 0x00,
        BiggerSizeOk = 0x01,
        MemoryOnly = 0x02,
        IconOnly = 0x04,
        ThumbnailOnly = 0x08,
        InCacheOnly = 0x10
    }

    [ComImport]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
    }

    [ComImport]
    [Guid("bcc18b79-ba16-442f-80e4-8cd659d057bc")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(
            [In, MarshalAs(UnmanagedType.Struct)] SIZE size,
            [In] SIIGBF flags,
            out IntPtr phbm);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE
    {
        public int cx;
        public int cy;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr pbc,
        [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        out IShellItem ppv);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    public static ImageSource? GetThumbnail(string filePath, int pixelSize, FileCategory category)
    {
        if (category == FileCategory.Image)
            return LoadImageThumbnail(filePath, pixelSize);

        return GetShellThumbnail(filePath, pixelSize);
    }

    public static ImageSource? GetLargePreview(string filePath, int pixelSize, FileCategory category)
    {
        if (category == FileCategory.Image)
            return LoadImageThumbnail(filePath, pixelSize);

        return GetShellThumbnail(filePath, pixelSize)
               ?? GetShellThumbnail(filePath, pixelSize, iconFallback: true);
    }

    private static ImageSource? GetShellThumbnail(string filePath, int pixelSize, bool iconFallback = false)
    {
        try
        {
            var iid = typeof(IShellItem).GUID;
            SHCreateItemFromParsingName(filePath, IntPtr.Zero, iid, out var item);

            if (item is not IShellItemImageFactory factory)
                return null;

            var size = new SIZE { cx = pixelSize, cy = pixelSize };
            var flags = iconFallback
                ? SIIGBF.ResizeToFit | SIIGBF.IconOnly
                : SIIGBF.ResizeToFit | SIIGBF.ThumbnailOnly;

            var hr = factory.GetImage(size, flags, out var hBitmap);

            if (hr != 0 || hBitmap == IntPtr.Zero)
                return null;

            try
            {
                using var bitmap = Image.FromHbitmap(hBitmap);
                return ConvertToImageSource(bitmap);
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource? LoadImageThumbnail(string filePath, int pixelSize)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = pixelSize;
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource ConvertToImageSource(Bitmap bitmap)
    {
        var hBitmap = bitmap.GetHbitmap();
        try
        {
            var source = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }
}
