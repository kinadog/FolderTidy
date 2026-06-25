namespace FolderTidy.Services;

using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

public static class TextPreviewService
{
    private const int MaxPreviewBytes = 512 * 1024;

    private static readonly HashSet<string> PlainTextExtensions =
    [
        ".txt", ".md", ".log", ".ini", ".cfg", ".conf", ".csv", ".tsv", ".rtf"
    ];

    public static async Task<string?> LoadPrettyTextAsync(string filePath, string extension)
    {
        var ext = NormalizeExtension(extension);

        return ext switch
        {
            ".json" => await LoadPrettyJsonAsync(filePath),
            ".xml" => await LoadPrettyXmlAsync(filePath),
            _ when PlainTextExtensions.Contains(ext) => await LoadPlainTextAsync(filePath),
            _ => null
        };
    }

    public static bool SupportsExtension(string extension)
    {
        var ext = NormalizeExtension(extension);
        return ext is ".json" or ".xml" || PlainTextExtensions.Contains(ext);
    }

    private static async Task<string?> LoadPlainTextAsync(string filePath)
        => await ReadLimitedTextAsync(filePath);

    private static async Task<string?> LoadPrettyJsonAsync(string filePath)
    {
        var text = await ReadLimitedTextAsync(filePath);
        if (text is null)
            return null;

        if (text.StartsWith("미리보기 제한:", StringComparison.Ordinal))
            return text;

        using var document = JsonDocument.Parse(text);
        return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
    }

    private static async Task<string?> LoadPrettyXmlAsync(string filePath)
    {
        var text = await ReadLimitedTextAsync(filePath);
        if (text is null)
            return null;

        if (text.StartsWith("미리보기 제한:", StringComparison.Ordinal))
            return text;

        var document = XDocument.Parse(text, LoadOptions.None);
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = false,
            Encoding = Encoding.UTF8
        };

        using var writer = new StringWriter();
        using (var xmlWriter = XmlWriter.Create(writer, settings))
            document.Save(xmlWriter);

        return writer.ToString();
    }

    private static async Task<string?> ReadLimitedTextAsync(string filePath)
    {
        var info = new FileInfo(filePath);
        if (!info.Exists || info.Length == 0)
            return null;

        if (info.Length > MaxPreviewBytes)
            return $"미리보기 제한: 파일이 너무 큽니다 ({info.Length:N0} bytes, 최대 {MaxPreviewBytes:N0} bytes).";

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync();
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return string.Empty;

        return extension.StartsWith('.')
            ? extension.ToLowerInvariant()
            : $".{extension.ToLowerInvariant()}";
    }
}
