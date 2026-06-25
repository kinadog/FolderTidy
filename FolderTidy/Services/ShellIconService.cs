namespace FolderTidy.Services;

using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public static class ShellIconService
{
    private const uint ShgfiIcon = 0x000000100;
    private const uint ShgfiSmallIcon = 0x000000001;
    private const uint ShgfiUseFileAttributes = 0x000000010;
    private const uint FileAttributeDirectory = 0x00000010;
    private const uint FileAttributeNormal = 0x00000080;

    private static readonly Dictionary<string, ImageSource> IconCache = new(StringComparer.OrdinalIgnoreCase);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Shfileinfo
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref Shfileinfo psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static ImageSource GetFolderIcon() =>
        GetCachedIcon("__folder__", () => GetIcon(null, isDirectory: true));

    public static ImageSource GetFileIcon(string extension) =>
        GetCachedIcon(extension, () => GetIcon(extension, isDirectory: false));

    public static ImageSource GetPathIcon(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return GetFileIcon(".file");

        if (!File.Exists(filePath))
            return GetFileIcon(Path.GetExtension(filePath));

        return GetCachedIcon($"path:{filePath}", () =>
        {
            var info = new Shfileinfo();
            var result = SHGetFileInfo(
                filePath,
                0,
                ref info,
                (uint)Marshal.SizeOf<Shfileinfo>(),
                ShgfiIcon | ShgfiSmallIcon);

            if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero)
                return GetFileIcon(Path.GetExtension(filePath));

            try
            {
                using var icon = Icon.FromHandle(info.hIcon);
                return ToImageSource(icon);
            }
            finally
            {
                DestroyIcon(info.hIcon);
            }
        });
    }

    public static ImageSource GetArchiveFileIcon(string archivePath)
    {
        if (!File.Exists(archivePath))
            return GetFileIcon(".zip");

        return GetCachedIcon($"archive:{archivePath}", () =>
        {
            var info = new Shfileinfo();
            var result = SHGetFileInfo(
                archivePath,
                0,
                ref info,
                (uint)Marshal.SizeOf<Shfileinfo>(),
                ShgfiIcon | ShgfiSmallIcon);

            if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero)
                return GetFileIcon(Path.GetExtension(archivePath));

            try
            {
                using var icon = Icon.FromHandle(info.hIcon);
                return ToImageSource(icon);
            }
            finally
            {
                DestroyIcon(info.hIcon);
            }
        });
    }

    private static ImageSource GetIcon(string? extension, bool isDirectory)
    {
        var path = isDirectory ? "dummy" : $"file{NormalizeExtension(extension)}";
        var attributes = isDirectory ? FileAttributeDirectory : FileAttributeNormal;
        var flags = ShgfiIcon | ShgfiSmallIcon | ShgfiUseFileAttributes;

        var info = new Shfileinfo();
        var result = SHGetFileInfo(path, attributes, ref info, (uint)Marshal.SizeOf<Shfileinfo>(), flags);

        if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero)
            return CreateFallbackIcon();

        try
        {
            using var icon = Icon.FromHandle(info.hIcon);
            return ToImageSource(icon);
        }
        finally
        {
            DestroyIcon(info.hIcon);
        }
    }

    private static ImageSource GetCachedIcon(string key, Func<ImageSource> factory)
    {
        if (IconCache.TryGetValue(key, out var cached))
            return cached;

        var icon = factory();
        icon.Freeze();
        IconCache[key] = icon;
        return icon;
    }

    private static ImageSource ToImageSource(Icon icon)
    {
        using var bitmap = icon.ToBitmap();
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

    private static ImageSource CreateFallbackIcon()
    {
        var visual = new System.Windows.Controls.TextBlock
        {
            Text = "📄",
            FontSize = 12,
            Width = 16,
            Height = 16,
            TextAlignment = TextAlignment.Center
        };
        visual.Measure(new System.Windows.Size(16, 16));
        visual.Arrange(new Rect(0, 0, 16, 16));

        var bitmap = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return ".file";

        return extension.StartsWith('.')
            ? extension.ToLowerInvariant()
            : $".{extension.ToLowerInvariant()}";
    }

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
}
