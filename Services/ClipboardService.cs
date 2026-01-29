using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DavidPortapales.Services;

public class ClipboardService
{
    private readonly IClipboard _clipboard;

    public ClipboardService(IClipboard clipboard)
    {
        _clipboard = clipboard;
    }

    public async Task<string?> GetTextAsync()
    {
        try
        {
#pragma warning disable CS0618
            return await _clipboard.GetTextAsync();
#pragma warning restore CS0618
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading text: {ex.Message}");
            return null;
        }
    }

    public async Task SetTextAsync(string text)
    {
        try
        {
            await _clipboard.SetTextAsync(text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting text: {ex.Message}");
        }
    }
    
    public async Task SetImageAsync(Bitmap image)
    {
        try
        {
#pragma warning disable CS0618
             var data = new DataObject();
            
            using (var ms = new MemoryStream())
            {
                image.Save(ms);
                var bytes = ms.ToArray();
                
                // Standard for Linux (X11/Wayland usually respect image/png)
                data.Set("image/png", new MemoryStream(bytes));
                
                // Windows compat
                data.Set("PNG", new MemoryStream(bytes));
            }
            
            // Fallback
            data.Set("Bitmap", image);
            
            await _clipboard.SetDataObjectAsync(data);
#pragma warning restore CS0618
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting image: {ex.Message}");
        }
    }

    public async Task<Bitmap?> GetImageAsync()
    {
        try
        {
            // 1. Check for Files first
#pragma warning disable CS0618
            // Use old API (returns string[]) to match DataFormats.FileNames (string)
            var formats = await _clipboard.GetFormatsAsync();
#pragma warning restore CS0618
            
            if (formats == null) return null;

#pragma warning disable CS0618
            if (formats.Contains(DataFormats.FileNames)) 
            {
                 var fileData = await _clipboard.GetDataAsync(DataFormats.FileNames); 
#pragma warning restore CS0618
                 
                 if (fileData is System.Collections.IEnumerable fileList)
                 {
                     foreach (var item in fileList) 
                     {
                         if (item is string path && File.Exists(path))
                         {
                             var ext = Path.GetExtension(path).ToLower();
                             if (new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".ico" }.Contains(ext))
                             {
                                 try 
                                 {
                                     return new Bitmap(path);
                                 }
                                 catch 
                                 {
                                 }
                             }
                         }
                     }
                 }
            }

            // 2. Check for Bitmap Data
            string[] imageFormats = { 
                "image/png", 
                "png", 
                "image/jpeg", 
                "image/bmp",
                "Bitmap", 
                "DeviceIndependentBitmap" 
            };
            
            foreach (var format in imageFormats)
            {
                if (formats.Any(f => string.Equals(f, format, StringComparison.OrdinalIgnoreCase)))
                {
                    try 
                    {
#pragma warning disable CS0618
                        var data = await _clipboard.GetDataAsync(format);
#pragma warning restore CS0618
                        
                        if (data is byte[] bytes)
                        {
                            return new Bitmap(new MemoryStream(bytes));
                        }
                        else if (data is Stream stream)
                        {
                            return new Bitmap(stream);
                        }
                        else if (data is Bitmap bmp)
                        {
                            return bmp;
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch (Exception ex) 
        {
             Console.WriteLine($"Error obtaining image: {ex.Message}");
        }
        return null;
    }

    public string ComputeImageHash(Bitmap bitmap)
    {
        try
        {
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms);
                var bytes = ms.ToArray();
                using (var sha256 = SHA256.Create())
                {
                    var hashBytes = sha256.ComputeHash(bytes);
                    return Convert.ToBase64String(hashBytes);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error computing hash: {ex.Message}");
            return string.Empty;
        }
    }
}
