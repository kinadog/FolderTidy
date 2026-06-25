namespace FolderTidy.Services;

using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Docnet.Core;
using Docnet.Core.Models;

public static class PdfPreviewService
{
    public static ImageSource? RenderFirstPage(string filePath, int maxDimension)
    {
        if (!File.Exists(filePath))
            return null;

        var bytes = File.ReadAllBytes(filePath);
        if (bytes.Length == 0)
            return null;

        using var docReader = DocLib.Instance.GetDocReader(bytes, new PageDimensions(maxDimension, maxDimension));

        if (docReader.GetPageCount() == 0)
            return null;

        using var pageReader = docReader.GetPageReader(0);
        var width = pageReader.GetPageWidth();
        var height = pageReader.GetPageHeight();
        var rawBytes = pageReader.GetImage();

        if (width <= 0 || height <= 0 || rawBytes.Length == 0)
            return null;

        var stride = width * 4;
        var bitmap = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            rawBytes,
            stride);
        bitmap.Freeze();
        return bitmap;
    }
}
