using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input; 
using Avalonia.Input.Platform;
using Avalonia; // For PixelPoint
using DavidPortapales.Services;

#pragma warning disable CS0618 // Suppress obsolete warnings

namespace DavidPortapales;

public partial class MainWindow : Window
{
    private bool _isMiniMode = true;
    
    private ClipboardService? _clipboardService;
    private HistoryManager? _historyManager;

    // Expose History for Binding
    public ObservableCollection<ClipboardItem> History => _historyManager?.History ?? new ObservableCollection<ClipboardItem>();

    public MainWindow()
    {
        InitializeComponent();
        // DataContext will be set in OnOpened after services are ready
        
        // Setup initial state
        // Initial state setup moved to OnOpened to ensure screen coordinates are accurate


        // Setup Double Click (DoubleTapped) 
        var miniWidget = this.FindControl<Border>("MiniWidget");
        if (miniWidget != null)
        {
            miniWidget.DoubleTapped += (s, e) => ToggleMode();
        }
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var clipboard = GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            _clipboardService = new ClipboardService(clipboard);
            _historyManager = new HistoryManager(_clipboardService, () => FlashWidget());
            
            // Start monitoring
            _historyManager.StartMonitoring();

            // Set DataContext now that History is available
            DataContext = this;
            
            // Wait for window manager to map the window
            await Task.Delay(200);

            // Apply initial mode and position
            SetWindowMode(true);
        }
        else
        {
            Console.WriteLine("Clipboard not available on init.");
        }
    }
    
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _historyManager?.StopMonitoring();
        base.OnClosing(e);
    }
    
    private void SetWindowMode(bool isMini)
    {
        // Capture screen *before* any resizing to improve accuracy
        var screen = Screens.ScreenFromVisual(this) ?? Screens.Primary ?? Screens.All.FirstOrDefault();

        _isMiniMode = isMini;
        
        var miniWidget = this.FindControl<Border>("MiniWidget");
        var mainView = this.FindControl<Border>("MainView");
        
        if (miniWidget != null) miniWidget.IsVisible = isMini;
        if (mainView != null) mainView.IsVisible = !isMini;

        if (isMini)
        {
            // Resize window to widget size
            this.Width = 50;
            this.Height = 50;
        }
        else
        {
            // Resize window to standard size
            this.Width = 600;
            this.Height = 500;
        }

        // Defer positioning to allow resize to complete/layout first.
        // This mitigates race conditions where the Window Manager might override the position.
        Dispatcher.UIThread.Post(() =>
        {
            if (screen != null)
            {
                var workingArea = screen.WorkingArea;
                PixelPoint newPos;

                if (isMini)
                {
                    // Fixed position: Always 120px padding from right + 50px width = 170px offset
                    var x = workingArea.X + workingArea.Width - 170; 
                    var y = workingArea.Y + 10; // 10px padding from top
                    newPos = new PixelPoint((int)x, (int)y);
                }
                else
                {
                    // Center on screen
                    var x = workingArea.X + (workingArea.Width - 600) / 2;
                    var y = workingArea.Y + (workingArea.Height - 500) / 2;
                    newPos = new PixelPoint((int)x, (int)y);
                }
                
                this.Position = newPos;
            }
        }, DispatcherPriority.Input);
    }
    
    private void ToggleMode()
    {
        // Toggle based on current state
        SetWindowMode(!_isMiniMode);
    }
    
    public void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
         SetWindowMode(true);
    }

    public void FlashWidget()
    {
        // Simple visual feedback: change widget background color briefly
        if (_isMiniMode)
        {
            Dispatcher.UIThread.Post(async () => {
                var miniWidget = this.FindControl<Border>("MiniWidget");
                if (miniWidget != null)
                {
                    var originalBrush = miniWidget.Background;
                    miniWidget.Background = Brushes.Yellow; // Highlight
                    await Task.Delay(200);
                    miniWidget.Background = originalBrush;
                }
            });
        }
    }

    public async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        if (_clipboardService == null || _historyManager == null) return;

        if (sender is Control control && control.DataContext is ClipboardItem item)
        {
            try 
            {
                if (item.IsText && item.TextContent != null)
                {
                    // Update manager state to avoid re-adding
                    _historyManager.UpdateLastText(item.TextContent);
                    
                    await _clipboardService.SetTextAsync(item.TextContent);
                }
                else if (item.IsImage && item.ImageContent != null)
                {
                    var hash = _clipboardService.ComputeImageHash(item.ImageContent);
                    _historyManager.UpdateLastImageHash(hash);
                    
                    await _clipboardService.SetImageAsync(item.ImageContent);
                }
                
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
