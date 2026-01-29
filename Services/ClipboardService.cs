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
            return await _clipboard.GetTextAsync();
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
            // 1. Check for Files first (User copied a file)
            var formats = await _clipboard.GetFormatsAsync();
            if (formats == null) return null;

            if (formats.Contains(DataFormats.FileNames))
            {
                 // Handle file drop (image files)
                 var fileData = await _clipboard.GetDataAsync(DataFormats.FileNames);
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
                                     // Failed to load this file, try next
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
                if (formats.Any(f => f.Equals(format, StringComparison.OrdinalIgnoreCase)))
                {
                    try 
                    {
                        var data = await _clipboard.GetDataAsync(format);
                        
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
                         // Ignore and try next format
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
