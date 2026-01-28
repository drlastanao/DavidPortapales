using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Avalonia.Input.Platform; // For IClipboard

namespace DavidPortapales;

public partial class MainWindow : Window
{
    private DispatcherTimer _timer;
    
    // Historial observable para la UI
    public ObservableCollection<ClipboardItem> History { get; set; } = new ObservableCollection<ClipboardItem>();

    // Para evitar duplicados
    private string? _lastText;
    private string? _lastImageHash; 

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        
        // Initialize the timer
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(5);
        _timer.Tick += Timer_Tick;
        _timer.Start();

        // Check immediately on startup
        CheckClipboard();
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

            // 1. Intentar obtener texto
            string? text = null;
            try 
            {
                text = await clipboard.GetTextAsync();
            }
            catch {} // Ignorar errores de texto

            bool textChanged = !string.IsNullOrEmpty(text) && text != _lastText;

            if (textChanged)
            {
                _lastText = text;
                _lastImageHash = null; // Reiniciar hash de imagen
                var item = new ClipboardItem(text!);
                History.Insert(0, item);
                return; // Priorizamos texto si cambió
            }

            // 2. Si no hay cambio de texto, intentar imagen
            // Solo si el último item no era el mismo texto (o si es null)
            // Aunque si el usuario copia lo mismo, el sistema suele actualizar el timestamp interno, pero content es igual.
            // Nuestra logica: solo guardar si es diferente.
            
            var image = await GetClipboardImageAsync(clipboard);
            if (image != null)
            {
                // Calcular hash para comparar
                var hash = ComputeImageHash(image);
                if (hash != _lastImageHash)
                {
                    _lastImageHash = hash;
                    _lastText = null; // Reiniciar texto
                    var item = new ClipboardItem(image);
                    History.Insert(0, item);
                }
            }
        }
        catch (Exception ex)
        {
            // Opcional: Loguear error en UI o consola
            Console.WriteLine($"Error al leer portapapeles: {ex.Message}");
        }
    }

    private async Task<Bitmap?> GetClipboardImageAsync(IClipboard clipboard)
    {
        try
        {
            var formats = await clipboard.GetFormatsAsync();
            if (formats == null) return null;

            // Lista de formatos comunes de imagen en Linux/Windows
            string[] imageFormats = { "image/png", "png", "image/jpeg", "image/bmp", "Bitmap" };
            
            foreach (var format in imageFormats)
            {
                if (formats.Any(f => f.Equals(format, StringComparison.OrdinalIgnoreCase)))
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
                    // A veces Avalonia devuelve ya un Bitmap si es formato nativo
                    else if (data is Bitmap bmp)
                    {
                        return bmp;
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
        // Guardar a stream para hashear los bytes
        // Esto puede ser costoso para imágenes grandes, pero necesario para comparar contendo real.
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
}