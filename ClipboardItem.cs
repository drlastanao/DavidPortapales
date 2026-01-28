using System;
using Avalonia.Media.Imaging;

namespace DavidPortapales;

public class ClipboardItem
{
    public DateTime Timestamp { get; set; }
    public string? TextContent { get; set; }
    public Bitmap? ImageContent { get; set; }
    public bool IsImage => ImageContent != null;
    public bool IsText => !IsImage;

    // "Fecha hora minutos segundos"
    public string TimestampDisplay => Timestamp.ToString("dd/MM/yyyy HH:mm:ss");

    public string TypeDisplay => IsImage ? "Imagen" : "Texto";

    // Metadata for Text
    public int CharCount => TextContent?.Length ?? 0;
    public int WordCount => TextContent?.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length ?? 0;

    // Metadata for Image
    public double ImageWidth => ImageContent?.PixelSize.Width ?? 0;
    public double ImageHeight => ImageContent?.PixelSize.Height ?? 0;
    
    public ClipboardItem(string text)
    {
        Timestamp = DateTime.Now;
        TextContent = text;
        ImageContent = null;
    }

    public ClipboardItem(Bitmap image)
    {
        Timestamp = DateTime.Now;
        TextContent = null;
        ImageContent = image;
    }
}
