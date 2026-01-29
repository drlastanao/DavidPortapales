using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Media.Imaging;

namespace DavidPortapales.Services;

public class HistoryManager
{
    public ObservableCollection<ClipboardItem> History { get; private set; } = new ObservableCollection<ClipboardItem>();
    
    private readonly ClipboardService _clipboardService;
    private DispatcherTimer _timer;
    private string? _lastText;
    private string? _lastImageHash;
    private Action? _onNewItemDetected; // Callback for UI updates (like flashing)

    public HistoryManager(ClipboardService clipboardService, Action? onNewItemDetected = null)
    {
        _clipboardService = clipboardService;
        _onNewItemDetected = onNewItemDetected;
        
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(1); 
        _timer.Tick += async (s, e) => await CheckClipboardAsync();
    }

    public void StartMonitoring()
    {
        _timer.Start();
        // Initial check
        Task.Run(async () => await CheckClipboardAsync());
    }

    public void StopMonitoring()
    {
        _timer.Stop();
    }

    public async Task CheckClipboardAsync()
    {
        // Check Text
        string? text = await _clipboardService.GetTextAsync();
        bool textChanged = !string.IsNullOrEmpty(text) && text != _lastText;

        if (textChanged)
        {
            _lastText = text;
            _lastImageHash = null; 
            
            // UI Update on Main Thread
            Dispatcher.UIThread.Post(() => {
                var item = new ClipboardItem(text!);
                History.Insert(0, item);
                _onNewItemDetected?.Invoke();
            });
            return; 
        }

        // Check Image
        var image = await _clipboardService.GetImageAsync();
        if (image != null)
        {
            var hash = _clipboardService.ComputeImageHash(image);
            if (hash != _lastImageHash && !string.IsNullOrEmpty(hash))
            {
                _lastImageHash = hash;
                _lastText = null; 
                
                // UI Update on Main Thread
                Dispatcher.UIThread.Post(() => {
                    var item = new ClipboardItem(image);
                    History.Insert(0, item);
                    _onNewItemDetected?.Invoke();
                });
            }
        }
    }

    public void ManualAdd(ClipboardItem item)
    {
        // For testing or manual addition
        History.Insert(0, item);
    }
    
    public void UpdateLastText(string text)
    {
        _lastText = text;
        _lastImageHash = null;
    }

    public void UpdateLastImageHash(string hash)
    {
        _lastImageHash = hash;
        _lastText = null;
    }
}
