using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using slidecraft.Models;
using slidecraft.Services;
using slidecraft.ViewModels;
using Xunit;
using P = DocumentFormat.OpenXml.Presentation;

namespace slidecraft.Tests;

/// <summary>
/// Round-trip tests per §10 of the spec: build a model, save to .pptx,
/// reopen, and verify data integrity.
/// </summary>
public sealed class RoundTripTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PptxExportService _exporter;
    private readonly PptxImportService _importer;

    /// <summary>Create a temporary directory for test files.</summary>
    public RoundTripTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "slidecraft_tests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _exporter = new PptxExportService();
        _importer = new PptxImportService();
    }

    /// <summary>Clean up temporary files.</summary>
    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private string TempFile(string name = "test.pptx") => Path.Combine(_tempDir, name);

    // ── Test 1: Full round-trip (§10.1) ──────────────────────────────

    /// <summary>
    /// Build a model with 3 slides (text with bold + colour, an embedded
    /// PNG, a Fade transition) → Save → Open → assert all data preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_ThreeSlides_PreservesAllData()
    {
        // Arrange
        var model = new PresentationModel();

        // Slide 1: text with bold + colour
        var slide1 = new SlideModel();
        slide1.Elements.Add(new TextElement
        {
            Text = "Hello World",
            X = 100, Y = 50, Width = 400, Height = 80,
            FontSize = 36, Bold = true, Italic = false,
            ColorHex = "FF0000"
        });
        model.Slides.Add(slide1);

        // Slide 2: embedded PNG image
        var slide2 = new SlideModel();
        var pngBytes = CreateMinimalPng();
        slide2.Elements.Add(new ImageElement
        {
            Data = pngBytes, Extension = "png",
            X = 120, Y = 60, Width = 300, Height = 200
        });
        model.Slides.Add(slide2);

        // Slide 3: Fade transition + text
        var slide3 = new SlideModel { Transition = TransitionType.Fade };
        slide3.Elements.Add(new TextElement
        {
            Text = "Slide Three",
            X = 80, Y = 40, Width = 500, Height = 60,
            FontSize = 24, Bold = false, Italic = true,
            ColorHex = "0000FF"
        });
        model.Slides.Add(slide3);

        var path = TempFile("roundtrip.pptx");

        // Act
        _exporter.Save(model, path);
        var (loaded, skipped) = _importer.Open(path);

        // Assert
        Assert.Equal(0, skipped);
        Assert.Equal(3, loaded.Slides.Count);

        // Slide 1 assertions
        var s1 = loaded.Slides[0];
        Assert.Single(s1.Elements);
        var t1 = Assert.IsType<TextElement>(s1.Elements[0]);
        Assert.Equal("Hello World", t1.Text);
        Assert.True(t1.Bold);
        Assert.False(t1.Italic);
        Assert.Equal("FF0000", t1.ColorHex);
        Assert.Equal(36, t1.FontSize, 0.1);
        Assert.Equal(100, t1.X, 1.0);
        Assert.Equal(50, t1.Y, 1.0);
        Assert.Equal(400, t1.Width, 1.0);
        Assert.Equal(80, t1.Height, 1.0);

        // Slide 2 assertions
        var s2 = loaded.Slides[1];
        Assert.Single(s2.Elements);
        var img = Assert.IsType<ImageElement>(s2.Elements[0]);
        Assert.True(img.Data.Length > 0, "Image bytes should be non-empty");
        Assert.Equal(120, img.X, 1.0);
        Assert.Equal(60, img.Y, 1.0);
        Assert.Equal(300, img.Width, 1.0);
        Assert.Equal(200, img.Height, 1.0);

        // Slide 3 assertions
        var s3 = loaded.Slides[2];
        Assert.Equal(TransitionType.Fade, s3.Transition);
        Assert.Single(s3.Elements);
        var t3 = Assert.IsType<TextElement>(s3.Elements[0]);
        Assert.Equal("Slide Three", t3.Text);
        Assert.True(t3.Italic);
        Assert.Equal("0000FF", t3.ColorHex);
    }

    // ── Test 2: Unsupported shapes (§10.3) ───────────────────────────

    /// <summary>
    /// Open a deck with unsupported shapes (§10.3) → no exception; skipped-count > 0.
    /// </summary>
    [Fact]
    public void Open_DeckWithUnsupportedShapes_IncrementsSkippedCountWithoutThrowing()
    {
        var model = new PresentationModel();
        var slide = new SlideModel();
        slide.Elements.Add(new TextElement
        {
            Text = "Slide with extra shapes",
            X = 10, Y = 10, Width = 200, Height = 40
        });
        model.Slides.Add(slide);

        var path = TempFile("unsupported.pptx");
        _exporter.Save(model, path);

        // Append an unsupported shape (a shape without a TextBody, e.g. a geometric shape or graphic) into the slide's ShapeTree
        using (var doc = PresentationDocument.Open(path, true))
        {
            var slidePart = doc.PresentationPart!.SlideParts.First();
            var shapeTree = slidePart.Slide!.CommonSlideData!.ShapeTree!;
            var unsupportedShape = new P.Shape(
                new P.NonVisualShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 99U, Name = "Unsupported Shape 99" },
                    new P.NonVisualShapeDrawingProperties(),
                    new P.ApplicationNonVisualDrawingProperties()));
            shapeTree.Append(unsupportedShape);
            slidePart.Slide.Save();
        }

        // Open and verify that the unsupported shape was skipped gracefully without exception
        var (loaded, skipped) = _importer.Open(path);
        Assert.True(skipped > 0, "Unsupported shape should increment skipped count");
        Assert.Single(loaded.Slides);
        Assert.Single(loaded.Slides[0].Elements);
    }

    // ── Test 3: Normal deck no skips ─────────────────────────────────

    /// <summary>
    /// Open a normal deck without unsupported shapes → 0 skips.
    /// </summary>
    [Fact]
    public void Open_NormalDeck_NoExceptionNoSkips()
    {
        var model = new PresentationModel();
        model.Slides.Add(new SlideModel());
        model.Slides[0].Elements.Add(new TextElement
        {
            Text = "Simple text", X = 10, Y = 10, Width = 200, Height = 40
        });

        var path = TempFile("noskip.pptx");
        _exporter.Save(model, path);

        var (loaded, skipped) = _importer.Open(path);
        Assert.Equal(0, skipped);
        Assert.Single(loaded.Slides);
    }

    // ── Test 4: OpenXml validation (§10.4) ───────────────────────────

    /// <summary>
    /// Validate output with OpenXmlValidator — should return no errors.
    /// </summary>
    [Fact]
    public void Save_ProducesValidOpenXml()
    {
        var model = new PresentationModel();
        var slide = new SlideModel { Transition = TransitionType.Wipe };
        slide.Elements.Add(new TextElement
        {
            Text = "Validation test",
            X = 50, Y = 50, Width = 300, Height = 60,
            FontSize = 20, Bold = true, ColorHex = "333333"
        });
        model.Slides.Add(slide);

        var path = TempFile("validate.pptx");
        _exporter.Save(model, path);

        using var doc = PresentationDocument.Open(path, false);
        var validator = new OpenXmlValidator();
        var errors = validator.Validate(doc).ToList();

        Assert.Empty(errors);
    }

    // ── Test 5: Empty presentation (edge case) ───────────────────────

    /// <summary>
    /// Save a presentation with one empty slide, reopen it.
    /// </summary>
    [Fact]
    public void RoundTrip_EmptySlide_Succeeds()
    {
        var model = new PresentationModel();
        model.Slides.Add(new SlideModel());

        var path = TempFile("empty.pptx");
        _exporter.Save(model, path);
        var (loaded, _) = _importer.Open(path);

        Assert.Single(loaded.Slides);
        Assert.Empty(loaded.Slides[0].Elements);
    }

    // ── Test 6: Multi-line text round-trip ────────────────────────────

    /// <summary>
    /// Text with newlines should round-trip correctly.
    /// </summary>
    [Fact]
    public void RoundTrip_MultiLineText_Preserved()
    {
        var model = new PresentationModel();
        var slide = new SlideModel();
        slide.Elements.Add(new TextElement
        {
            Text = "Line One\nLine Two\nLine Three",
            X = 10, Y = 10, Width = 400, Height = 100,
            FontSize = 18
        });
        model.Slides.Add(slide);

        var path = TempFile("multiline.pptx");
        _exporter.Save(model, path);
        var (loaded, _) = _importer.Open(path);

        var txt = Assert.IsType<TextElement>(loaded.Slides[0].Elements[0]);
        Assert.Equal("Line One\nLine Two\nLine Three", txt.Text);
    }

    // ── Test 7: Undo and Redo with polymorphic serialization ─────────

    /// <summary>
    /// Verify Undo and Redo snapshots with polymorphic SlideElement models.
    /// </summary>
    [Fact]
    public void UndoRedo_RestoresPreviousAndNextState()
    {
        var vm = new MainViewModel();
        Assert.Single(vm.Slides);

        // Action 1: Add a textbox
        vm.AddTextBoxCommand.Execute(null);
        Assert.Single(vm.CurrentSlide!.Elements);

        // Action 2: Add another slide
        vm.AddSlideCommand.Execute(null);
        Assert.Equal(2, vm.Slides.Count);

        // Undo Action 2 -> back to 1 slide
        vm.UndoCommand.Execute(null);
        Assert.Single(vm.Slides);

        // Redo Action 2 -> forward to 2 slides
        vm.RedoCommand.Execute(null);
        Assert.Equal(2, vm.Slides.Count);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>Create a minimal valid 1×1 PNG file (67 bytes).</summary>
    private static byte[] CreateMinimalPng()
    {
        // Minimal 1×1 white PNG
        return [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
            0xDE,
            0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54, // IDAT chunk
            0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00, 0x00,
            0x00, 0x02, 0x00, 0x01, 0xE2, 0x21, 0xBC, 0x33,
            0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, // IEND chunk
            0xAE, 0x42, 0x60, 0x82
        ];
    }
}