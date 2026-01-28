using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Avalonia.Input; 
using Avalonia.Input.Platform;
using Avalonia; // For PixelPoint

#pragma warning disable CS0618 // Suppress obsolete warnings

namespace DavidPortapales;

public partial class MainWindow : Window
{
    private DispatcherTimer _timer;
    private bool _isMiniMode = true;
    
    public ObservableCollection<ClipboardItem> History { get; set; } = new ObservableCollection<ClipboardItem>();

    private string? _lastText;
    private string? _lastImageHash; 

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        
        // Setup initial state
        SetMiniMode();

        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(1); // Faster check for responsiveness
        _timer.Tick += Timer_Tick;
        _timer.Start();

        CheckClipboard();
        
        // Setup Double Click (DoubleTapped) 
        var miniWidget = this.FindControl<Border>("MiniWidget");
        if (miniWidget != null)
        {
            miniWidget.DoubleTapped += (s, e) => ToggleMode();
        }
    }
    
    private void SetMiniMode()
    {
        _isMiniMode = true;
        
        var miniWidget = this.FindControl<Border>("MiniWidget");
        var mainView = this.FindControl<Border>("MainView");
        
        if (miniWidget != null) miniWidget.IsVisible = true;
        if (mainView != null) mainView.IsVisible = false;

        // Resize window to widget size
        this.Width = 50;
        this.Height = 50;
        
        // Position at top center
        var screen = Screens.Primary ?? Screens.All.FirstOrDefault();
        if (screen != null)
        {
            var workingArea = screen.WorkingArea;
            var x = workingArea.X + (workingArea.Width - 50) / 2;
            var y = workingArea.Y + 10; // 10px padding from top
            this.Position = new PixelPoint(x, y);
        }
    }

    private void SetExpandedMode()
    {
        _isMiniMode = false;
        
        var miniWidget = this.FindControl<Border>("MiniWidget");
        var mainView = this.FindControl<Border>("MainView");
        
        if (miniWidget != null) miniWidget.IsVisible = false;
        if (mainView != null) mainView.IsVisible = true;

        // Resize window to standard size
        this.Width = 600;
        this.Height = 500;
        
        // Center on screen
        var screen = Screens.Primary ?? Screens.All.FirstOrDefault();
        if (screen != null)
        {
            var workingArea = screen.WorkingArea;
            var x = workingArea.X + (workingArea.Width - 600) / 2;
            var y = workingArea.Y + (workingArea.Height - 500) / 2;
            this.Position = new PixelPoint(x, y);
        }
    }
    
    private void ToggleMode()
    {
        if (_isMiniMode) SetExpandedMode();
        else SetMiniMode();
    }
    
    public void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
         SetMiniMode();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        CheckClipboard();
    }

    private async void CheckClipboard()
    {
        try
        {
            var clipboard = GetTopLevel(this)?.Clipboard;
            if (clipboard == null) return;

            // Use TryGetTextAsync if possible to avoid exceptions
            // But stick to what worked with pragmas.
            string? text = await clipboard.GetTextAsync(); 

            bool textChanged = !string.IsNullOrEmpty(text) && text != _lastText;

            if (textChanged)
            {
                _lastText = text;
                _lastImageHash = null; 
                var item = new ClipboardItem(text!);
                History.Insert(0, item);
                FlashWidget();
                return; 
            }

            var image = await GetClipboardImageAsync(clipboard);
            if (image != null)
            {
                var hash = ComputeImageHash(image);
                if (hash != _lastImageHash)
                {
                    _lastImageHash = hash;
                    _lastText = null; 
                    var item = new ClipboardItem(image);
                    History.Insert(0, item);
                    FlashWidget();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al leer portapapeles: {ex.Message}");
        }
    }
    
    private async void FlashWidget()
    {
        // Simple visual feedback: change widget background color briefly
        if (_isMiniMode)
        {
            var miniWidget = this.FindControl<Border>("MiniWidget");
            if (miniWidget != null)
            {
                var originalBrush = miniWidget.Background;
                miniWidget.Background = Brushes.Yellow; // Highlight
                await Task.Delay(200);
                miniWidget.Background = originalBrush;
            }
        }
    }

    private async Task<Bitmap?> GetClipboardImageAsync(IClipboard clipboard)
    {
        try
        {
            // 1. Check for Files first (User copied a file)
            var formats = await clipboard.GetFormatsAsync();
            if (formats == null) return null;

            if (formats.Contains(DataFormats.FileNames))
            {
                 // Handle file drop (image files)
                 var fileData = await clipboard.GetDataAsync(DataFormats.FileNames);
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
            // Prioritize standard formats that are easier to parse (PNG/JPEG)
            string[] imageFormats = { 
                "image/png", 
                "png", 
                "image/jpeg", 
                "image/bmp",
                "Bitmap", // Internal or simple
                "DeviceIndependentBitmap" // Often problematic without headers
            };
            
            foreach (var format in imageFormats)
            {
                if (formats.Any(f => f.Equals(format, StringComparison.OrdinalIgnoreCase)))
                {
                    try 
                    {
                        var data = await clipboard.GetDataAsync(format);
                        
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
                        // Some formats (like DIB) might be present but unparsable as simple Bitmap
                        // Ignore and try next format
                        // Console.WriteLine($"Debug: Failed to parse format {format}");
                    }
                }
            }
        }
        catch (Exception ex) 
        {
             Console.WriteLine($"Error obteniendo imagen: {ex.Message}");
        }
        return null;
    }

    private string ComputeImageHash(Bitmap bitmap)
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

    public async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control control && control.DataContext is ClipboardItem item)
        {
            try 
            {
                var clipboard = GetTopLevel(this)?.Clipboard;
                if (clipboard == null) return;

                if (item.IsText && item.TextContent != null)
                {
                    _lastText = item.TextContent; 
                    _lastImageHash = null;
                    
                    await clipboard.SetTextAsync(item.TextContent);
                }
                else if (item.IsImage && item.ImageContent != null)
                {
                    _lastImageHash = ComputeImageHash(item.ImageContent);
                    _lastText = null;

                    var data = new DataObject();
                    
                    using (var ms = new MemoryStream())
                    {
                        item.ImageContent.Save(ms);
                        var bytes = ms.ToArray();
                        
                        // Standard for Linux (X11/Wayland usually respect image/png)
                        data.Set("image/png", new MemoryStream(bytes));
                        
                        // Windows compat
                        data.Set("PNG", new MemoryStream(bytes));
                    }
                    
                    // Fallback
                    data.Set("Bitmap", item.ImageContent);
                    
                    await clipboard.SetDataObjectAsync(data);

                }
                
                // Optional: Flash on copy back too
                FlashWidget();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al copiar al portapapeles: {ex.Message}");
            }
        }

    }

    public async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
         if (sender is Control control && control.DataContext is ClipboardItem item && item.IsImage && item.ImageContent != null)
         {
             try 
             {
                 var topLevel = GetTopLevel(this);
                 if (topLevel == null) return;

                 var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
                 {
                     Title = "Guardar Imagen",
                     DefaultExtension = "png",
                     FileTypeChoices = new[]
                     {
                        new Avalonia.Platform.Storage.FilePickerFileType("PNG Image") { Patterns = new[] { "*.png" } },
                        new Avalonia.Platform.Storage.FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                     }
                 });

                 if (file != null)
                 {
                     using (var stream = await file.OpenWriteAsync())
                     {
                         item.ImageContent.Save(stream);
                     }
                     FlashWidget();
                 }
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"Error al guardar imagen: {ex.Message}");
             }
         }
    }
}
