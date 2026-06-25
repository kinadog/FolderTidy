namespace FolderTidy.Services;

using FolderTidy.Models;

public static class FileCategoryClassifier
{
    private static readonly HashSet<string> Installers =
    [
        ".exe", ".msi", ".msix", ".msu", ".cab", ".appx", ".appxbundle", ".msp"
    ];

    private static readonly HashSet<string> Archives =
    [
        ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".tgz", ".iso", ".img"
    ];

    private static readonly HashSet<string> Images =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".ico", ".svg", ".tiff", ".tif", ".heic", ".avif"
    ];

    private static readonly HashSet<string> GraphicDesign =
    [
        // Adobe
        ".ai", ".ait", ".eps", ".psd", ".psb", ".indd", ".indt", ".idml", ".abr", ".aco", ".ase", ".atn",
        // Sketch / UI design
        ".sketch", ".xd", ".fig",
        // Affinity
        ".afdesign", ".afphoto", ".afpub",
        // Corel / 기타 래스터·벡터 작업
        ".cdr", ".cdt", ".cmx", ".xcf", ".kra", ".clip", ".sai", ".procreate",
        // 3D/모션 그래픽 프로젝트 (DCC)
        ".blend", ".c4d", ".max", ".ma", ".mb", ".zpr"
    ];

    private static readonly HashSet<string> CadDrawing =
    [
        // AutoCAD
        ".dwg", ".dxf", ".dwf", ".dwt", ".dwfx",
        // MicroStation / Bentley
        ".dgn", ".cel",
        // Revit / BIM
        ".rvt", ".rfa", ".rte", ".ifc", ".nwd", ".nwc",
        // SketchUp
        ".skp", ".layout",
        // SolidWorks
        ".sldprt", ".sldasm", ".slddrw",
        // Inventor
        ".ipt", ".iam", ".idw", ".ipn",
        // Fusion 360 / Autodesk
        ".f3d", ".f3z",
        // CATIA
        ".catpart", ".catproduct", ".catdrawing",
        // NX / Unigraphics
        ".prt", ".asm",
        // 교환 포맷
        ".step", ".stp", ".iges", ".igs", ".stl", ".3mf",
        // Rhino / FreeCAD / OpenSCAD
        ".3dm", ".fcstd", ".scad",
        // ACIS / Parasolid
        ".sat", ".x_t", ".x_b",
        // 플롯 / 레이저
        ".plt", ".hpgl",
        // Altium / EDA 도면
        ".sch", ".pcb", ".brd"
    ];

    private static readonly HashSet<string> Documents =
    [
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".md", ".rtf", ".hwp", ".hwpx", ".odt", ".csv"
    ];

    private static readonly HashSet<string> Videos =
    [
        ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".webm", ".m4v", ".flv", ".mpeg", ".mpg"
    ];

    private static readonly HashSet<string> Audio =
    [
        ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a", ".opus"
    ];

    private static readonly HashSet<string> Code =
    [
        ".cs", ".js", ".ts", ".jsx", ".tsx", ".py", ".html", ".htm", ".css", ".scss", ".json", ".xml", ".yaml", ".yml", ".sql", ".java", ".cpp", ".h", ".go", ".rs", ".php", ".rb", ".swift", ".kt"
    ];

    public static FileCategory Classify(string extension)
    {
        var ext = extension.ToLowerInvariant();
        if (!ext.StartsWith('.'))
            ext = $".{ext}";

        if (Installers.Contains(ext)) return FileCategory.Installer;
        if (Archives.Contains(ext)) return FileCategory.Archive;
        if (Images.Contains(ext)) return FileCategory.Image;
        if (GraphicDesign.Contains(ext)) return FileCategory.GraphicDesign;
        if (CadDrawing.Contains(ext)) return FileCategory.CadDrawing;
        if (Documents.Contains(ext)) return FileCategory.Document;
        if (Videos.Contains(ext)) return FileCategory.Video;
        if (Audio.Contains(ext)) return FileCategory.Audio;
        if (Code.Contains(ext)) return FileCategory.Code;

        return FileCategory.Other;
    }
}
