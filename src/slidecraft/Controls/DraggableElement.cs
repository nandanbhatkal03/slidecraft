using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using slidecraft.ViewModels;

namespace slidecraft.Controls;

/// <summary>
/// A content control placed on the slide canvas that supports click-to-select,
/// drag-to-move, corner-thumb resize, double-click-to-edit (text), and Delete
/// key removal. Communicates position/size changes back to
/// <see cref="ElementViewModel"/>.
/// </summary>
public sealed class DraggableElement : ContentControl
{
    /// <summary>The element view model this control represents.</summary>
    public ElementViewModel ViewModel { get; }

    private bool _isDragging;
    private Point _dragStart;
    private double _startX, _startY;

    private bool _isSelected;
    private ResizeAdorner? _adorner;

    // For text editing
    private TextBlock? _textBlock;
    private TextBox? _editBox;
    private bool _isEditing;

    /// <summary>Raised when this element is clicked/selected.</summary>
    public event Action<DraggableElement>? Selected;

    /// <summary>Raised when the user presses Delete on a selected element.</summary>
    public event Action<DraggableElement>? DeleteRequested;

    /// <summary>Raised when a property changes that should dirty the presentation.</summary>
    public event Action? Modified;

    /// <summary>Allows external adorners to trigger the Modified event.</summary>
    public void NotifyModified() => Modified?.Invoke();

    /// <summary>Helper to resolve the MainViewModel from the parent window.</summary>
    public MainViewModel? GetMainViewModel() =>
        Window.GetWindow(this)?.DataContext as MainViewModel;

    /// <summary>Whether this element is currently selected (shows resize handles).</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;

            if (value)
                ShowAdorner();
            else
            {
                HideAdorner();
                CommitTextEdit();
            }
        }
    }

    /// <summary>Creates a draggable element for the given view model.</summary>
    public DraggableElement(ElementViewModel vm)
    {
        ViewModel = vm;
        DataContext = vm;
        Focusable = true;

        Canvas.SetLeft(this, vm.X);
        Canvas.SetTop(this, vm.Y);
        Width = vm.Width;
        Height = vm.Height;

        CreateContent();

        vm.PropertyChanged += OnViewModelPropertyChanged;

        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseMove += OnMouseMove;
        KeyDown += OnKeyDown;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ElementViewModel.X):
                Canvas.SetLeft(this, ViewModel.X);
                break;
            case nameof(ElementViewModel.Y):
                Canvas.SetTop(this, ViewModel.Y);
                break;
            case nameof(ElementViewModel.Width):
                Width = ViewModel.Width;
                break;
            case nameof(ElementViewModel.Height):
                Height = ViewModel.Height;
                break;
            case nameof(ElementViewModel.Text):
            case nameof(ElementViewModel.FontSize):
            case nameof(ElementViewModel.Bold):
            case nameof(ElementViewModel.Italic):
            case nameof(ElementViewModel.ColorHex):
                RefreshVisual();
                break;
        }
    }

    // ── Content creation ─────────────────────────────────────────────

    private void CreateContent()
    {
        if (ViewModel.IsText)
        {
            _textBlock = new TextBlock
            {
                Text = ViewModel.Text,
                FontSize = ViewModel.FontSize,
                FontWeight = ViewModel.Bold ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = ViewModel.Italic ? FontStyles.Italic : FontStyles.Normal,
                Foreground = BrushFromHex(ViewModel.ColorHex),
                TextWrapping = TextWrapping.Wrap,
                IsHitTestVisible = false
            };
            Content = _textBlock;
        }
        else if (ViewModel.IsImage && ViewModel.ImageSource != null)
        {
            var img = new System.Windows.Controls.Image
            {
                Source = ViewModel.ImageSource,
                Stretch = Stretch.Uniform,
                IsHitTestVisible = false
            };
            Content = img;
        }
    }

    /// <summary>Refresh the visual to match the current ViewModel state.</summary>
    public void RefreshVisual()
    {
        if (_textBlock != null)
        {
            _textBlock.Text = ViewModel.Text;
            _textBlock.FontSize = ViewModel.FontSize;
            _textBlock.FontWeight = ViewModel.Bold ? FontWeights.Bold : FontWeights.Normal;
            _textBlock.FontStyle = ViewModel.Italic ? FontStyles.Italic : FontStyles.Normal;
            _textBlock.Foreground = BrushFromHex(ViewModel.ColorHex);
        }
    }

    // ── Selection adorner ────────────────────────────────────────────

    private void ShowAdorner()
    {
        var layer = AdornerLayer.GetAdornerLayer(this);
        if (layer != null && _adorner == null)
        {
            _adorner = new ResizeAdorner(this);
            layer.Add(_adorner);
        }
    }

    private void HideAdorner()
    {
        if (_adorner != null)
        {
            var layer = AdornerLayer.GetAdornerLayer(this);
            layer?.Remove(_adorner);
            _adorner = null;
        }
    }

    // ── Drag to move ─────────────────────────────────────────────────

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Double-click to edit text
        if (e.ClickCount == 2 && ViewModel.IsText && !_isEditing)
        {
            BeginTextEdit();
            e.Handled = true;
            return;
        }

        Selected?.Invoke(this);
        Focus();

        _isDragging = true;
        _dragStart = e.GetPosition(Parent as UIElement);
        _startX = Canvas.GetLeft(this);
        _startY = Canvas.GetTop(this);
        CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        ReleaseMouseCapture();

        double finalX = Canvas.GetLeft(this);
        double finalY = Canvas.GetTop(this);

        if (Math.Abs(finalX - _startX) > 0.5 || Math.Abs(finalY - _startY) > 0.5)
        {
            // Position changed, push snapshot first
            GetMainViewModel()?.PushUndoSnapshot();
            ViewModel.X = finalX;
            ViewModel.Y = finalY;
            Modified?.Invoke();
        }
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
        var pos = e.GetPosition(Parent as UIElement);
        var dx = pos.X - _dragStart.X;
        var dy = pos.Y - _dragStart.Y;

        var newX = Math.Max(0, Math.Min(_startX + dx, 960 - Width));
        var newY = Math.Max(0, Math.Min(_startY + dy, 540 - Height));

        Canvas.SetLeft(this, newX);
        Canvas.SetTop(this, newY);
    }

    // ── Delete key ───────────────────────────────────────────────────

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && _isSelected && !_isEditing)
        {
            DeleteRequested?.Invoke(this);
            e.Handled = true;
        }
    }

    // ── Inline text editing ──────────────────────────────────────────

    private void BeginTextEdit()
    {
        if (_isEditing) return;
        _isEditing = true;

        _editBox = new TextBox
        {
            Text = ViewModel.Text,
            FontSize = ViewModel.FontSize,
            FontWeight = ViewModel.Bold ? FontWeights.Bold : FontWeights.Normal,
            FontStyle = ViewModel.Italic ? FontStyles.Italic : FontStyles.Normal,
            Foreground = BrushFromHex(ViewModel.ColorHex),
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Padding = new Thickness(0)
        };
        _editBox.LostFocus += (_, _) => CommitTextEdit();
        Content = _editBox;
        _editBox.Focus();
        _editBox.SelectAll();
    }

    private void CommitTextEdit()
    {
        if (!_isEditing || _editBox == null) return;
        _isEditing = false;

        if (ViewModel.Text != _editBox.Text)
        {
            GetMainViewModel()?.PushUndoSnapshot();
            ViewModel.Text = _editBox.Text;
            if (_textBlock != null)
                _textBlock.Text = ViewModel.Text;
            Modified?.Invoke();
        }

        Content = _textBlock;
    }

    // ── Helpers ──────────────────────────────────────────────────────

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

// ═══════════════════════════════════════════════════════════════════════
// Resize adorner — four corner thumbs for resizing elements
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Adorner that draws four small squares at the corners of the adorned
/// element. Dragging any corner resizes the element.
/// </summary>
internal sealed class ResizeAdorner : Adorner
{
    private readonly VisualCollection _visuals;

    // Corner thumb visuals
    private readonly Rectangle _topLeft, _topRight, _bottomLeft, _bottomRight;

    private const double ThumbSize = 8;

    // Drag state
    private bool _isDragging;
    private Point _dragStart;
    private double _origX, _origY, _origW, _origH;
    private string _activeCorner = "";

    /// <summary>Creates the adorner with four corner handles.</summary>
    public ResizeAdorner(UIElement adornedElement) : base(adornedElement)
    {
        _visuals = new VisualCollection(this);

        _topLeft = MakeThumb("TL");
        _topRight = MakeThumb("TR");
        _bottomLeft = MakeThumb("BL");
        _bottomRight = MakeThumb("BR");

        _visuals.Add(_topLeft);
        _visuals.Add(_topRight);
        _visuals.Add(_bottomLeft);
        _visuals.Add(_bottomRight);
    }

    private Rectangle MakeThumb(string tag)
    {
        var r = new Rectangle
        {
            Width = ThumbSize,
            Height = ThumbSize,
            Fill = Brushes.White,
            Stroke = Brushes.DodgerBlue,
            StrokeThickness = 1.5,
            Tag = tag,
            Cursor = tag switch
            {
                "TL" => Cursors.SizeNWSE,
                "TR" => Cursors.SizeNESW,
                "BL" => Cursors.SizeNESW,
                "BR" => Cursors.SizeNWSE,
                _ => Cursors.Arrow
            }
        };
        r.MouseLeftButtonDown += OnThumbMouseDown;
        r.MouseLeftButtonUp += OnThumbMouseUp;
        r.MouseMove += OnThumbMouseMove;
        return r;
    }

    // ── Thumb drag ───────────────────────────────────────────────────

    private void OnThumbMouseDown(object sender, MouseButtonEventArgs e)
    {
        var thumb = (Rectangle)sender;
        _activeCorner = (string)thumb.Tag;
        _isDragging = true;

        var el = (DraggableElement)AdornedElement;
        _origX = Canvas.GetLeft(el);
        _origY = Canvas.GetTop(el);
        _origW = el.Width;
        _origH = el.Height;
        _dragStart = e.GetPosition(el.Parent as UIElement);

        thumb.CaptureMouse();
        e.Handled = true;
    }

    private void OnThumbMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        var thumb = (Rectangle)sender;
        thumb.ReleaseMouseCapture();

        var el = (DraggableElement)AdornedElement;
        double newX = Canvas.GetLeft(el);
        double newY = Canvas.GetTop(el);
        double newW = el.Width;
        double newH = el.Height;

        if (Math.Abs(newX - _origX) > 0.5 || Math.Abs(newY - _origY) > 0.5 ||
            Math.Abs(newW - _origW) > 0.5 || Math.Abs(newH - _origH) > 0.5)
        {
            el.GetMainViewModel()?.PushUndoSnapshot();
            el.ViewModel.X = newX;
            el.ViewModel.Y = newY;
            el.ViewModel.Width = newW;
            el.ViewModel.Height = newH;
            el.NotifyModified();
        }
        e.Handled = true;
    }

    private void OnThumbMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        var el = (DraggableElement)AdornedElement;
        var pos = e.GetPosition(el.Parent as UIElement);
        double dx = pos.X - _dragStart.X;
        double dy = pos.Y - _dragStart.Y;

        const double min = 20;

        switch (_activeCorner)
        {
            case "BR":
                el.Width = Math.Max(min, _origW + dx);
                el.Height = Math.Max(min, _origH + dy);
                break;
            case "BL":
                el.Width = Math.Max(min, _origW - dx);
                el.Height = Math.Max(min, _origH + dy);
                Canvas.SetLeft(el, _origX + (_origW - el.Width));
                break;
            case "TR":
                el.Width = Math.Max(min, _origW + dx);
                el.Height = Math.Max(min, _origH - dy);
                Canvas.SetTop(el, _origY + (_origH - el.Height));
                break;
            case "TL":
                el.Width = Math.Max(min, _origW - dx);
                el.Height = Math.Max(min, _origH - dy);
                Canvas.SetLeft(el, _origX + (_origW - el.Width));
                Canvas.SetTop(el, _origY + (_origH - el.Height));
                break;
        }

        InvalidateVisual();
    }

    // ── Layout & rendering ───────────────────────────────────────────

    /// <inheritdoc />
    protected override int VisualChildrenCount => _visuals.Count;

    /// <inheritdoc />
    protected override Visual GetVisualChild(int index) => _visuals[index];

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var w = AdornedElement.RenderSize.Width;
        var h = AdornedElement.RenderSize.Height;
        double half = ThumbSize / 2;

        _topLeft.Arrange(new Rect(-half, -half, ThumbSize, ThumbSize));
        _topRight.Arrange(new Rect(w - half, -half, ThumbSize, ThumbSize));
        _bottomLeft.Arrange(new Rect(-half, h - half, ThumbSize, ThumbSize));
        _bottomRight.Arrange(new Rect(w - half, h - half, ThumbSize, ThumbSize));

        return finalSize;
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        // Draw a selection border
        var pen = new Pen(Brushes.DodgerBlue, 1.5) { DashStyle = DashStyles.Dash };
        var rect = new Rect(AdornedElement.RenderSize);
        drawingContext.DrawRectangle(null, pen, rect);
    }
}
