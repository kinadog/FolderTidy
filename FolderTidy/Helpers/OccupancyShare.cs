namespace FolderTidy.Helpers;

public static class OccupancyShare
{
    public static double ComputePercent(long sizeBytes, long totalBytes)
    {
        if (sizeBytes <= 0 || totalBytes <= 0)
            return 0;

        return sizeBytes * 100.0 / totalBytes;
    }

    public static string FormatPercent(double percent)
    {
        if (percent <= 0)
            return "0%";

        if (percent >= 10)
            return $"{percent:F1}%";

        if (percent >= 0.01)
            return $"{percent:F2}%";

        return "<0.01%";
    }

    public static void Apply(long sizeBytes, long totalBytes, out double percent, out string percentText)
    {
        percent = ComputePercent(sizeBytes, totalBytes);
        percentText = FormatPercent(percent);
    }
}
