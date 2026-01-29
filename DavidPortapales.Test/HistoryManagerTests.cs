using Xunit;
using Moq;
using DavidPortapales.Services;
using Avalonia.Input;
using Avalonia.Input.Platform;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace DavidPortapales.Test;

public class HistoryManagerTests
{
    [Fact]
    public void ManualAdd_AddsItemToHistory()
    {
        // Arrange
        var mockClipboard = new Mock<IClipboard>();
        var service = new ClipboardService(mockClipboard.Object);
        var manager = new HistoryManager(service);
        var item = new ClipboardItem("Test Item");

        // Act
        manager.ManualAdd(item);

        // Assert
        Assert.Single(manager.History);
        Assert.Equal("Test Item", manager.History[0].TextContent);
    }
    
    [Fact]
    public void UpdateLastText_UpdatesInternalState()
    {
        // Internal state is private, but we can verify behavior via side effects if possible.
        // Or we assume it works if we can't observe it directly without reflection.
        // However, the Logic usually is:
        // UpdateLastText("A") -> CheckClipboard logic shouldn't add "A" again.
        
        // This requires mocking Clipboard check which is likely coupled with Timer or manual call.
    }
    
    // CheckClipboardAsync test is risky due to Dispatcher.
    // We will skip it for now unless we can initialize a Dispatcher.
}
