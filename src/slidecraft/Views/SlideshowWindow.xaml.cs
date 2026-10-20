using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using slidecraft.ViewModels;

namespace slidecraft.Views;

/// <summary>
/// Full-screen slideshow window. Navigates slides with arrow keys and
/// mouse clicks; closes on Escape. Renders each slide's elements as
/// static (non-interactive) visuals.
/// </summary>
public partial class SlideshowWindow : Window
{
    private readonly List<SlideViewModel> _slides;
    private int _currentIndex;

    /// <summary>Creates a slideshow starting at the given slide index.</summary>
    public SlideshowWindow(IList<SlideViewModel> slides, int startIndex)
    {
        InitializeComponent();
        _slides = new List<SlideViewModel>(slides);
        _currentIndex = Math.Clamp(startIndex, 0, Math.Max(0, _slides.Count - 1));
        Loaded += (_, _) => RenderSlide();
    }

    // ── Navigation ───────────────────────────────────────────────────

    /// <summary>Handle keyboard navigation and Escape to close.</summary>
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Right:
            case Key.Down:
            case Key.Space:
            case Key.PageDown:
                GoNext();
                break;
            case Key.Left:
            case Key.Up:
            case Key.PageUp:
                GoPrevious();
                break;
            case Key.Home:
                _currentIndex = 0;
                RenderSlide();
                break;
            case Key.End:
                _currentIndex = _slides.Count - 1;
                RenderSlide();
                break;
            case Key.Escape:
                Close();
                break;
        }
        e.Handled = true;
    }

    /// <summary>Advance to next slide on mouse click.</summary>
    private void OnMouseClick(object sender, MouseButtonEventArgs e)
    {
        GoNext();
        e.Handled = true;
    }

    private void GoNext()
    {
        if (_currentIndex < _slides.Count - 1)
        {
            _currentIndex++;
            RenderSlide();
        }
        else
        {
            Close(); // end of slideshow
        }
    }

    private void GoPrevious()
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            RenderSlide();
        }
    }

    // ── Rendering ────────────────────────────────────────────────────

    /// <summary>Render the current slide's elements onto the canvas.</summary>
    private void RenderSlide()
    {
        SlideCanvas.Children.Clear();
        if (_currentIndex < 0 || _currentIndex >= _slides.Count) return;

        var slide = _slides[_currentIndex];
        foreach (var element in slide.Elements)
        {
            UIElement visual;
            if (element.IsText)
            {
                visual = new TextBlock
                {
                    Text = element.Text,
                    FontSize = element.FontSize,
                    FontWeight = element.Bold ? FontWeights.Bold : FontWeights.Normal,
                    FontStyle = element.Italic ? FontStyles.Italic : FontStyles.Normal,
                    Foreground = BrushFromHex(element.ColorHex),
                    TextWrapping = TextWrapping.Wrap,
                    Width = element.Width,
                    Height = element.Height
                };
            }
            else if (element.IsImage && element.ImageSource != null)
            {
                visual = new Image
                {
                    Source = element.ImageSource,
                    Stretch = Stretch.Uniform,
                    Width = element.Width,
                    Height = element.Height
                };
            }
            else
            {
                continue;
            }

            Canvas.SetLeft(visual, element.X);
            Canvas.SetTop(visual, element.Y);
            SlideCanvas.Children.Add(visual);
        }
    }

    /// <summary>Create a brush from a hex colour string.</summary>
    private static SolidColorBrush BrushFromHex(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString("#" + hex);
            return new SolidColorBrush(color);
        }
        catch
        {
            return Brushes.Black;
        }
    }
}
