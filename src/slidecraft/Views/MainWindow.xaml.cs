using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using slidecraft.Controls;
using slidecraft.Models;
using slidecraft.Services;
using slidecraft.ViewModels;

namespace slidecraft.Views;

/// <summary>
/// Code-behind for the main application window. Handles pure UI concerns:
/// canvas rendering, element selection, status bar updates.
/// All business logic lives in <see cref="MainViewModel"/>.
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel _viewModel = null!;
    private bool _suppressEvents;

    /// <summary>Initialises the main window, creates the ViewModel and wires events.</summary>
    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.CanvasRefreshRequested += RenderCanvas;
        _viewModel.StatusUpdateRequested += () =>
        {
            UpdateStatusBar();
            UpdateTitleBar();
        };
        _viewModel.SlideshowRequested += OnSlideshowRequested;

        // Initial render
        RenderCanvas();
        UpdateStatusBar();
        UpdateTitleBar();
    }

    // ── ViewModel property changes ───────────────────────────────────

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.CurrentSlide):
                RenderCanvas();
                UpdateStatusBar();
                SyncTransitionCombo();
                break;
            case nameof(MainViewModel.IsDirty):
                UpdateTitleBar();
                UpdateStatusBar();
                break;
            case nameof(MainViewModel.SelectedElement):
                FormatBarControl.SyncToSelection(_viewModel.SelectedElement);
                break;
        }
    }

    // ── Canvas rendering ─────────────────────────────────────────────

    /// <summary>
    /// Clear the canvas and re-create visual elements for the current slide.
    /// Preserves element selection if still present.
    /// </summary>
    private void RenderCanvas()
    {
        SlideCanvas.Children.Clear();
        var slide = _viewModel.CurrentSlide;
        if (slide == null) return;

        DraggableElement? elementToSelect = null;

        foreach (var element in slide.Elements)
        {
            var draggable = new DraggableElement(element);
            draggable.Selected += OnElementSelected;
            draggable.DeleteRequested += OnElementDeleteRequested;
            draggable.Modified += () =>
            {
                _viewModel.IsDirty = true;
                UpdateTitleBar();
                UpdateStatusBar();
            };
            SlideCanvas.Children.Add(draggable);

            if (_viewModel.SelectedElement != null && element == _viewModel.SelectedElement)
            {
                elementToSelect = draggable;
            }
        }

        if (elementToSelect != null)
        {
            elementToSelect.IsSelected = true;
            FormatBarControl.SyncToSelection(elementToSelect.ViewModel);
        }
    }

    /// <summary>Handle element selection on the canvas.</summary>
    private void OnElementSelected(DraggableElement element)
    {
        // Deselect all others
        foreach (var child in SlideCanvas.Children)
        {
            if (child is DraggableElement de && de != element)
                de.IsSelected = false;
        }
        element.IsSelected = true;
        _viewModel.SelectedElement = element.ViewModel;
        FormatBarControl.SyncToSelection(element.ViewModel);
    }

    /// <summary>Handle Delete key on a selected element.</summary>
    private void OnElementDeleteRequested(DraggableElement element)
    {
        _viewModel.SelectedElement = element.ViewModel;
        _viewModel.DeleteElementCommand.Execute(null);
    }

    /// <summary>Click on empty canvas area deselects all elements.</summary>
    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Only if the click was directly on the canvas (not on a child)
        if (e.OriginalSource == SlideCanvas)
        {
            foreach (var child in SlideCanvas.Children)
            {
                if (child is DraggableElement de)
                    de.IsSelected = false;
            }
            _viewModel.SelectedElement = null;
        }
    }

    // ── Thumbnail panel ──────────────────────────────────────────────

    /// <summary>Re-render canvas when a different thumbnail is selected.</summary>
    private void OnThumbnailSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // The binding handles CurrentSlide update; PropertyChanged triggers RenderCanvas
    }

    // ── Layout dropdown ─────────────────────────────────────────────

    /// <summary>Apply the chosen layout when the layout dropdown selection changes.</summary>
    private void OnLayoutChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents || _viewModel.CurrentSlide == null) return;
        if (LayoutCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            _viewModel.ApplyLayoutCommand.Execute(tag);
        }
    }

    // ── Transition dropdown ──────────────────────────────────────────

    /// <summary>Update the current slide's transition when the dropdown changes.</summary>
    private void OnTransitionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents || _viewModel.CurrentSlide == null) return;
        if (TransitionCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            var transition = tag switch
            {
                "Fade" => TransitionType.Fade,
                "Push" => TransitionType.Push,
                "Wipe" => TransitionType.Wipe,
                _ => TransitionType.None
            };
            if (_viewModel.CurrentSlide.Transition != transition)
            {
                _viewModel.PushUndoSnapshot();
                _viewModel.CurrentSlide.Transition = transition;
                _viewModel.IsDirty = true;
                UpdateTitleBar();
                UpdateStatusBar();
            }
        }
    }

    /// <summary>Sync the transition combo to the current slide's value without marking dirty.</summary>
    private void SyncTransitionCombo()
    {
        if (_viewModel.CurrentSlide == null) return;
        _suppressEvents = true;
        try
        {
            TransitionCombo.SelectedIndex = (int)_viewModel.CurrentSlide.Transition;
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    // ── Status bar & title ───────────────────────────────────────────

    /// <summary>Update the status bar text.</summary>
    private void UpdateStatusBar()
    {
        var loc = LocalizationService.Instance;
        var format = loc.Get("Status_SlideOf");
        var slideIndex = _viewModel.CurrentSlide != null
            ? _viewModel.Slides.IndexOf(_viewModel.CurrentSlide) + 1
            : 0;
        StatusSlideInfo.Text = string.Format(format, slideIndex, _viewModel.Slides.Count);
        StatusDirty.Text = _viewModel.IsDirty ? loc.Get("Status_Unsaved") : "";
    }

    /// <summary>Update the title bar with dirty indicator.</summary>
    private void UpdateTitleBar()
    {
        var loc = LocalizationService.Instance;
        var baseTitle = loc.Get("AppTitle");
        if (_viewModel.Presentation.FilePath != null)
            baseTitle += " — " + System.IO.Path.GetFileName(_viewModel.Presentation.FilePath);
        if (_viewModel.IsDirty)
            baseTitle = "* " + baseTitle;
        Title = baseTitle;
    }

    // ── Window events ────────────────────────────────────────────────

    /// <summary>Open the slideshow window starting at the current slide.</summary>
    private void OnSlideshowRequested()
    {
        var startIdx = _viewModel.CurrentSlide != null
            ? _viewModel.Slides.IndexOf(_viewModel.CurrentSlide)
            : 0;
        var window = new SlideshowWindow(_viewModel.Slides, startIdx);
        window.Owner = this;
        window.ShowDialog();
    }

    /// <summary>Handles the Exit menu item click.</summary>
    private void OnExitClick(object sender, RoutedEventArgs e) => Close();

    /// <summary>Prompt to save on window close if dirty.</summary>
    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (!_viewModel.IsDirty) return;

        var loc = LocalizationService.Instance;
        var result = MessageBox.Show(
            loc.Get("Msg_SaveChanges"),
            loc.Get("Msg_Confirm"),
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        switch (result)
        {
            case MessageBoxResult.Yes:
                _viewModel.SaveCommand.Execute(null);
                if (_viewModel.IsDirty) e.Cancel = true; // save was cancelled
                break;
            case MessageBoxResult.Cancel:
                e.Cancel = true;
                break;
            // No → close without saving
        }
    }
}
