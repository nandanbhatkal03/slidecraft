using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using slidecraft.Models;
using slidecraft.Services;

namespace slidecraft.ViewModels;

/// <summary>
/// Main application view model. Owns the presentation model, the observable
/// slide collection, and all commands for file, slide, element and format
/// operations as specified in §7 of the design spec.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly PptxExportService _exportService = new();
    private readonly PptxImportService _importService = new();
    private readonly DialogService _dialogService = new();

    // ── Observable state ─────────────────────────────────────────────

    /// <summary>The underlying presentation model.</summary>
    [ObservableProperty] private PresentationModel _presentation = new();

    /// <summary>Currently selected slide.</summary>
    [ObservableProperty] private SlideViewModel? _currentSlide;

    /// <summary>Currently selected element on the active slide.</summary>
    [ObservableProperty] private ElementViewModel? _selectedElement;

    /// <summary>Whether the presentation has unsaved changes.</summary>
    [ObservableProperty] private bool _isDirty;

    /// <summary>Observable list of slide ViewModels for the thumbnail panel.</summary>
    public ObservableCollection<SlideViewModel> Slides { get; } = new();

    /// <summary>Raised to request the view to re-render the canvas.</summary>
    public event Action? CanvasRefreshRequested;

    /// <summary>Raised to request the view to update the status bar.</summary>
    public event Action? StatusUpdateRequested;

    // ── Constructor ──────────────────────────────────────────────────

    /// <summary>Creates the view model and initialises a new presentation.</summary>
    public MainViewModel()
    {
        New();
    }

    // ── File commands ────────────────────────────────────────────────

    /// <summary>Create a new presentation with one blank slide.</summary>
    [RelayCommand]
    private void New()
    {
        if (IsDirty && !ConfirmDiscardChanges()) return;

        Presentation = new PresentationModel();
        Slides.Clear();
        SelectedElement = null;

        AddSlide(null); // always start with one blank slide

        IsDirty = false;
        StatusUpdateRequested?.Invoke();
    }

    /// <summary>Open an existing .pptx file.</summary>
    [RelayCommand]
    private void Open()
    {
        if (IsDirty && !ConfirmDiscardChanges()) return;

        var path = _dialogService.ShowOpenDialog();
        if (path == null) return;

        try
        {
            var (model, skipped) = _importService.Open(path);
            Presentation = model;
            Slides.Clear();
            SelectedElement = null;

            for (int i = 0; i < model.Slides.Count; i++)
                Slides.Add(new SlideViewModel(model.Slides[i], i + 1));

            // Ensure at least one slide
            if (Slides.Count == 0)
                AddSlide(null);

            CurrentSlide = Slides.FirstOrDefault();
            IsDirty = false;

            if (skipped > 0)
                _dialogService.ShowInfo("Msg_SkippedElements");

            CanvasRefreshRequested?.Invoke();
            StatusUpdateRequested?.Invoke();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError("Msg_OpenFailed", ex.Message);
        }
    }

    /// <summary>Save the presentation to its current path, or prompt for one.</summary>
    [RelayCommand]
    private void Save()
    {
        if (Presentation.FilePath == null)
        {
            SaveAs();
            return;
        }
        DoSave(Presentation.FilePath);
    }

    /// <summary>Prompt for a path and save the presentation.</summary>
    [RelayCommand]
    private void SaveAs()
    {
        var path = _dialogService.ShowSaveDialog(Presentation.FilePath);
        if (path == null) return;
        DoSave(path);
    }

    /// <summary>Perform the actual save operation.</summary>
    private void DoSave(string path)
    {
        try
        {
            _exportService.Save(Presentation, path);
            Presentation.FilePath = path;
            IsDirty = false;
            _dialogService.ShowInfo("Msg_SavedOk");
            StatusUpdateRequested?.Invoke();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError("Msg_SaveFailed", ex.Message);
        }
    }

    // ── Slide commands ───────────────────────────────────────────────

    /// <summary>Add a new slide with an optional layout.</summary>
    [RelayCommand]
    private void AddSlide(string? layoutKey)
    {
        var slideModel = new SlideModel();
        LayoutFactory.Apply(slideModel, layoutKey);
        Presentation.Slides.Add(slideModel);

        var vm = new SlideViewModel(slideModel, Slides.Count + 1);
        Slides.Add(vm);
        CurrentSlide = vm;
        IsDirty = true;

        CanvasRefreshRequested?.Invoke();
        StatusUpdateRequested?.Invoke();
    }

    /// <summary>Delete the current slide.</summary>
    [RelayCommand]
    private void DeleteSlide()
    {
        if (CurrentSlide == null || Slides.Count <= 1) return;

        var idx = Slides.IndexOf(CurrentSlide);
        Presentation.Slides.Remove(CurrentSlide.Model);
        Slides.Remove(CurrentSlide);
        RenumberSlides();

        CurrentSlide = Slides[Math.Min(idx, Slides.Count - 1)];
        IsDirty = true;

        CanvasRefreshRequested?.Invoke();
        StatusUpdateRequested?.Invoke();
    }

    /// <summary>Duplicate the current slide.</summary>
    [RelayCommand]
    private void DuplicateSlide()
    {
        if (CurrentSlide == null) return;

        var srcModel = CurrentSlide.Model;
        var newModel = new SlideModel { Transition = srcModel.Transition };

        foreach (var el in srcModel.Elements)
        {
            switch (el)
            {
                case TextElement t:
                    newModel.Elements.Add(new TextElement
                    {
                        X = t.X, Y = t.Y, Width = t.Width, Height = t.Height,
                        Text = t.Text, FontSize = t.FontSize,
                        Bold = t.Bold, Italic = t.Italic, ColorHex = t.ColorHex
                    });
                    break;
                case ImageElement img:
                    newModel.Elements.Add(new ImageElement
                    {
                        X = img.X, Y = img.Y, Width = img.Width, Height = img.Height,
                        Data = (byte[])img.Data.Clone(), Extension = img.Extension
                    });
                    break;
            }
        }

        var idx = Slides.IndexOf(CurrentSlide) + 1;
        Presentation.Slides.Insert(idx, newModel);
        var vm = new SlideViewModel(newModel, idx + 1);
        Slides.Insert(idx, vm);
        RenumberSlides();

        CurrentSlide = vm;
        IsDirty = true;

        CanvasRefreshRequested?.Invoke();
        StatusUpdateRequested?.Invoke();
    }

    /// <summary>Move the current slide up (earlier) in the list.</summary>
    [RelayCommand]
    private void MoveSlideUp()
    {
        if (CurrentSlide == null) return;
        var idx = Slides.IndexOf(CurrentSlide);
        if (idx <= 0) return;

        Slides.Move(idx, idx - 1);
        (Presentation.Slides[idx], Presentation.Slides[idx - 1]) =
            (Presentation.Slides[idx - 1], Presentation.Slides[idx]);
        RenumberSlides();
        IsDirty = true;
        StatusUpdateRequested?.Invoke();
    }

    /// <summary>Move the current slide down (later) in the list.</summary>
    [RelayCommand]
    private void MoveSlideDown()
    {
        if (CurrentSlide == null) return;
        var idx = Slides.IndexOf(CurrentSlide);
        if (idx < 0 || idx >= Slides.Count - 1) return;

        Slides.Move(idx, idx + 1);
        (Presentation.Slides[idx], Presentation.Slides[idx + 1]) =
            (Presentation.Slides[idx + 1], Presentation.Slides[idx]);
        RenumberSlides();
        IsDirty = true;
        StatusUpdateRequested?.Invoke();
    }

    // ── Element commands ─────────────────────────────────────────────

    /// <summary>Add a text box to the current slide.</summary>
    [RelayCommand]
    private void AddTextBox()
    {
        if (CurrentSlide == null) return;

        var textEl = new TextElement
        {
            X = 100, Y = 100, Width = 300, Height = 60,
            Text = LocalizationService.Instance.Get("Str_ClickToAddText"),
            FontSize = 24
        };
        var vm = CurrentSlide.AddElement(textEl);
        SelectedElement = vm;
        IsDirty = true;
        CanvasRefreshRequested?.Invoke();
    }

    /// <summary>Add an image to the current slide via file dialog.</summary>
    [RelayCommand]
    private void AddImage()
    {
        if (CurrentSlide == null) return;

        var path = _dialogService.ShowImageDialog();
        if (path == null) return;

        try
        {
            var data = File.ReadAllBytes(path);
            var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
            if (ext == "jpeg") ext = "jpg";

            // Determine display size: fit within 400 × 300 keeping aspect ratio
            double maxW = 400, maxH = 300;
            using var ms = new MemoryStream(data);
            var bmp = new System.Windows.Media.Imaging.BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = ms;
            bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();

            double w = bmp.PixelWidth;
            double h = bmp.PixelHeight;
            double scale = Math.Min(maxW / w, maxH / h);
            if (scale > 1) scale = 1; // don't upscale
            w *= scale;
            h *= scale;

            var imgEl = new ImageElement
            {
                X = 100, Y = 100, Width = w, Height = h,
                Data = data, Extension = ext
            };
            var vm = CurrentSlide.AddElement(imgEl);
            SelectedElement = vm;
            IsDirty = true;
            CanvasRefreshRequested?.Invoke();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError("Msg_OpenFailed", ex.Message);
        }
    }

    /// <summary>Delete the currently selected element.</summary>
    [RelayCommand]
    private void DeleteElement()
    {
        if (CurrentSlide == null || SelectedElement == null) return;
        CurrentSlide.RemoveElement(SelectedElement);
        SelectedElement = null;
        IsDirty = true;
        CanvasRefreshRequested?.Invoke();
    }

    // ── Format commands ──────────────────────────────────────────────

    /// <summary>Toggle bold on the selected text element.</summary>
    [RelayCommand]
    private void ToggleBold()
    {
        if (SelectedElement is { IsText: true })
        {
            SelectedElement.Bold = !SelectedElement.Bold;
            IsDirty = true;
            CanvasRefreshRequested?.Invoke();
        }
    }

    /// <summary>Toggle italic on the selected text element.</summary>
    [RelayCommand]
    private void ToggleItalic()
    {
        if (SelectedElement is { IsText: true })
        {
            SelectedElement.Italic = !SelectedElement.Italic;
            IsDirty = true;
            CanvasRefreshRequested?.Invoke();
        }
    }

    // ── Stubs for later phases ───────────────────────────────────────

    /// <summary>Start slideshow mode (stub — implemented in step 10).</summary>
    [RelayCommand]
    private void StartSlideshow()
    {
        // Will be implemented in step 10
    }

    /// <summary>Undo the last action (stub — implemented in step 10).</summary>
    [RelayCommand]
    private void Undo()
    {
        // Will be implemented in step 10
    }

    /// <summary>Redo the last undone action (stub — implemented in step 10).</summary>
    [RelayCommand]
    private void Redo()
    {
        // Will be implemented in step 10
    }

    /// <summary>Switch UI language (stub — implemented in Phase 2).</summary>
    [RelayCommand]
    private void SwitchLanguage(string? cultureCode)
    {
        // Will be implemented in Phase 2
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>Re-number all slides after reordering.</summary>
    private void RenumberSlides()
    {
        for (int i = 0; i < Slides.Count; i++)
            Slides[i].SlideNumber = i + 1;
    }

    /// <summary>
    /// Prompt the user to save unsaved changes.
    /// Returns <c>true</c> if it is safe to discard / proceed.
    /// </summary>
    private bool ConfirmDiscardChanges()
    {
        var result = _dialogService.ShowConfirm("Msg_SaveChanges");
        if (result == true)
        {
            Save();
            return !IsDirty; // false if save was cancelled
        }
        return result == false; // No → discard, Cancel → abort
    }
}
