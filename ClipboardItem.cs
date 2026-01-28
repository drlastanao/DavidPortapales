using System;
using Avalonia.Media.Imaging;

namespace DavidPortapales;

public class ClipboardItem
{
    public DateTime Timestamp { get; set; }
    public string? TextContent { get; set; }
    public Bitmap? ImageContent { get; set; }
    public bool IsImage => ImageContent != null;
    
    // Formato para mostrar la hora
    public string TimestampDisplay => Timestamp.ToString("HH:mm:ss");
    
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

    public bool IsText => !IsImage;
}
