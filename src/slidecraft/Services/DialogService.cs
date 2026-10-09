using Microsoft.Win32;
using System.Windows;

namespace slidecraft.Services;

/// <summary>
/// Thin wrapper around Win32 file dialogs and WPF message boxes,
/// making them mockable for tests and centralising filter strings.
/// </summary>
public class DialogService
{
    private readonly LocalizationService _loc = LocalizationService.Instance;

    /// <summary>Show a Save File dialog for .pptx files.</summary>
    /// <returns>The chosen path, or <c>null</c> if cancelled.</returns>
    public virtual string? ShowSaveDialog(string? initialPath = null)
    {
        var dlg = new SaveFileDialog
        {
            Filter = _loc.Get("Dlg_PptxFilter"),
            DefaultExt = ".pptx",
            FileName = initialPath ?? "Presentation.pptx"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    /// <summary>Show an Open File dialog for .pptx files.</summary>
    /// <returns>The chosen path, or <c>null</c> if cancelled.</returns>
    public virtual string? ShowOpenDialog()
    {
        var dlg = new OpenFileDialog
        {
            Filter = _loc.Get("Dlg_PptxFilter"),
            DefaultExt = ".pptx"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    /// <summary>Show an Open File dialog for image files.</summary>
    /// <returns>The chosen path, or <c>null</c> if cancelled.</returns>
    public virtual string? ShowImageDialog()
    {
        var dlg = new OpenFileDialog
        {
            Title = _loc.Get("Dlg_ImageTitle"),
            Filter = _loc.Get("Dlg_ImageFilter")
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    /// <summary>Show an informational message box.</summary>
    public virtual void ShowInfo(string messageKey)
    {
        var msg = _loc.Get(messageKey);
        var title = _loc.Get("AppTitle");
        MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>Show an error message box.</summary>
    public virtual void ShowError(string messageKey, string? detail = null)
    {
        var msg = _loc.Get(messageKey);
        if (detail != null) msg += "\n\n" + detail;
        var title = _loc.Get("AppTitle");
        MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>
    /// Show a Yes/No/Cancel confirmation dialog.
    /// Returns <c>true</c> for Yes, <c>false</c> for No, <c>null</c> for Cancel.
    /// </summary>
    public virtual bool? ShowConfirm(string messageKey)
    {
        var msg = _loc.Get(messageKey);
        var title = _loc.Get("Msg_Confirm");
        var result = MessageBox.Show(msg, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        return result switch
        {
            MessageBoxResult.Yes => true,
            MessageBoxResult.No => false,
            _ => null
        };
    }
}
