using CommunityToolkit.Mvvm.ComponentModel;
using slidecraft.Models;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace slidecraft.ViewModels;

/// <summary>
/// ViewModel wrapper around a <see cref="SlideElement"/>. Exposes all
/// properties for data binding and syncs changes back to the model.
/// </summary>
public partial class ElementViewModel : ObservableObject
{
    /// <summary>The underlying model element.</summary>
    public SlideElement Model { get; }

    /// <summary>Whether this element is a <see cref="TextElement"/>.</summary>
    public bool IsText => Model is TextElement;

    /// <summary>Whether this element is an <see cref="ImageElement"/>.</summary>
    public bool IsImage => Model is ImageElement;

    // ── Position / size ──────────────────────────────────────────────

    [ObservableProperty] private double _x;
    partial void OnXChanged(double value) => Model.X = value;

    [ObservableProperty] private double _y;
    partial void OnYChanged(double value) => Model.Y = value;

    [ObservableProperty] private double _width;
    partial void OnWidthChanged(double value) => Model.Width = value;

    [ObservableProperty] private double _height;
    partial void OnHeightChanged(double value) => Model.Height = value;

    // ── Text-specific ────────────────────────────────────────────────

    [ObservableProperty] private string _text = "";
    partial void OnTextChanged(string value)
    {
        if (Model is TextElement t) t.Text = value;
    }

    [ObservableProperty] private double _fontSize = 24;
    partial void OnFontSizeChanged(double value)
    {
        if (Model is TextElement t) t.FontSize = value;
    }

    [ObservableProperty] private bool _bold;
    partial void OnBoldChanged(bool value)
    {
        if (Model is TextElement t) t.Bold = value;
    }

    [ObservableProperty] private bool _italic;
    partial void OnItalicChanged(bool value)
    {
        if (Model is TextElement t) t.Italic = value;
    }

    [ObservableProperty] private string _colorHex = "000000";
    partial void OnColorHexChanged(string value)
    {
        if (Model is TextElement t) t.ColorHex = value;
    }

    // ── Image-specific ───────────────────────────────────────────────

    [ObservableProperty] private ImageSource? _imageSource;

    /// <summary>Creates a new <see cref="ElementViewModel"/> wrapping the given model.</summary>
    public ElementViewModel(SlideElement element)
    {
        Model = element;
        _x = element.X;
        _y = element.Y;
        _width = element.Width;
        _height = element.Height;

        if (element is TextElement txt)
        {
            _text = txt.Text;
            _fontSize = txt.FontSize;
            _bold = txt.Bold;
            _italic = txt.Italic;
            _colorHex = txt.ColorHex;
        }
        else if (element is ImageElement img)
        {
            _imageSource = LoadImage(img.Data);
        }
    }

    /// <summary>Decode image bytes into a WPF <see cref="BitmapImage"/>.</summary>
    private static BitmapImage? LoadImage(byte[] data)
    {
        if (data.Length == 0) return null;
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = new MemoryStream(data);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }
}
