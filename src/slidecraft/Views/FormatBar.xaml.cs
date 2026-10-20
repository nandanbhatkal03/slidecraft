using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using slidecraft.ViewModels;

namespace slidecraft.Views;

/// <summary>
/// Code-behind for the format bar user control.
/// Handles font size and colour changes from the ComboBoxes
/// and forwards them to the selected element ViewModel.
/// </summary>
public partial class FormatBar : UserControl
{
    private bool _suppressEvents;

    /// <summary>Creates the format bar.</summary>
    public FormatBar()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sync the font size and colour ComboBoxes to the selected element's values.
    /// Called by the parent window whenever the selection changes.
    /// </summary>
    public void SyncToSelection(ElementViewModel? element)
    {
        _suppressEvents = true;
        try
        {
            if (element is { IsText: true })
            {
                // Font size
                FontSizeCombo.Text = element.FontSize.ToString("0");

                // Colour
                var hex = element.ColorHex.ToUpperInvariant();
                foreach (ComboBoxItem item in ColorCombo.Items)
                {
                    var tag = (item.Tag as string ?? "").TrimStart('#').ToUpperInvariant();
                    // Handle named colours
                    if (tag == "BLACK") tag = "000000";
                    else if (tag == "WHITE") tag = "FFFFFF";

                    if (tag == hex)
                    {
                        ColorCombo.SelectedItem = item;
                        break;
                    }
                }
            }
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    /// <summary>Handle font size change from the ComboBox.</summary>
    private void OnFontSizeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents) return;
        var vm = GetMainViewModel();
        if (vm?.SelectedElement is not { IsText: true } sel) return;

        string? text = null;
        if (FontSizeCombo.SelectedItem is ComboBoxItem item)
            text = item.Content?.ToString();
        else
            text = FontSizeCombo.Text;

        if (double.TryParse(text, out double size) && size >= 6 && size <= 200)
        {
            sel.FontSize = size;
            vm.IsDirty = true;
            vm.CanvasRefreshRequested?.Invoke();
        }
    }

    /// <summary>Handle colour change from the ComboBox.</summary>
    private void OnColorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents) return;
        var vm = GetMainViewModel();
        if (vm?.SelectedElement is not { IsText: true } sel) return;

        if (ColorCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            string hex = tag.TrimStart('#');
            // Handle named colours
            if (hex.Equals("Black", StringComparison.OrdinalIgnoreCase)) hex = "000000";
            else if (hex.Equals("White", StringComparison.OrdinalIgnoreCase)) hex = "FFFFFF";

            sel.ColorHex = hex;
            vm.IsDirty = true;
            vm.CanvasRefreshRequested?.Invoke();
        }
    }

    /// <summary>Get the MainViewModel from the DataContext hierarchy.</summary>
    private MainViewModel? GetMainViewModel()
    {
        return DataContext as MainViewModel
            ?? (DataContext as FrameworkElement)?.DataContext as MainViewModel
            ?? Window.GetWindow(this)?.DataContext as MainViewModel;
    }
}
