using DocumentFormat.OpenXml.Packaging;
using slidecraft.Helpers;
using slidecraft.Models;
using System.IO;
using P = DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;

namespace slidecraft.Services;

/// <summary>
/// Imports a .pptx file into a <see cref="PresentationModel"/> using the
/// Open XML SDK. Unsupported shapes (charts, SmartArt, tables) are silently
/// skipped and their count is returned.
/// </summary>
public sealed class PptxImportService : IPptxService
{
    /// <inheritdoc />
    public void Save(PresentationModel model, string path)
        => throw new NotSupportedException("Use PptxExportService for saving.");

    /// <inheritdoc />
    public (PresentationModel Model, int SkippedCount) Open(string path)
    {
        var model = new PresentationModel { FilePath = path };
        int skipped = 0;

        using var doc = PresentationDocument.Open(path, false);
        var presPart = doc.PresentationPart;
        if (presPart is null) return (model, skipped);
        var slideIdList = presPart.Presentation?.SlideIdList;
        if (slideIdList is null) return (model, skipped);

        foreach (var slideId in slideIdList.Elements<P.SlideId>())
        {
            var relId = slideId.RelationshipId!.Value!;
            var slidePart = (SlidePart)presPart.GetPartById(relId);
            var slideModel = new SlideModel();

            var shapeTree = slidePart.Slide?.CommonSlideData?.ShapeTree;
            if (shapeTree != null)
            {
                foreach (var child in shapeTree.ChildElements)
                {
                    switch (child)
                    {
                        case P.Shape shape:
                            var textEl = ReadTextShape(shape);
                            if (textEl != null)
                                slideModel.Elements.Add(textEl);
                            else
                                skipped++;
                            break;

                        case P.Picture pic:
                            var imgEl = ReadPicture(slidePart, pic);
                            if (imgEl != null)
                                slideModel.Elements.Add(imgEl);
                            else
                                skipped++;
                            break;

                        case P.NonVisualGroupShapeProperties:
                        case P.GroupShapeProperties:
                            // Expected structural elements — not skipped
                            break;

                        default:
                            // Charts, SmartArt, tables, group shapes, etc.
                            skipped++;
                            break;
                    }
                }
            }

            // Read transition
            var transition = slidePart.Slide?.GetFirstChild<P.Transition>();
            if (transition != null)
                slideModel.Transition = ReadTransition(transition);

            model.Slides.Add(slideModel);
        }

        return (model, skipped);
    }

    // ── Text shapes ──────────────────────────────────────────────────

    /// <summary>Read a <see cref="P.Shape"/> into a <see cref="TextElement"/>.</summary>
    private static TextElement? ReadTextShape(P.Shape shape)
    {
        var textBody = shape.TextBody;
        if (textBody == null) return null;

        // Join paragraphs with newline
        var paragraphs = textBody.Elements<D.Paragraph>().ToList();
        var text = string.Join("\n",
            paragraphs.Select(p => string.Concat(p.Elements<D.Run>().Select(r => r.Text?.Text ?? ""))));

        // Read formatting from the first run
        double fontSize = 24;
        bool bold = false, italic = false;
        string colorHex = "000000";

        var firstRun = paragraphs
            .SelectMany(p => p.Elements<D.Run>())
            .FirstOrDefault();

        if (firstRun?.RunProperties is { } rp)
        {
            if (rp.FontSize is not null)
                fontSize = rp.FontSize.Value / 100.0;
            bold = rp.Bold?.Value ?? false;
            italic = rp.Italic?.Value ?? false;

            var solidFill = rp.GetFirstChild<D.SolidFill>();
            var rgb = solidFill?.GetFirstChild<D.RgbColorModelHex>();
            if (rgb?.Val?.Value is string hex)
                colorHex = hex;
        }

        // Read position from Transform2D
        double x = 60, y = 40, w = 840, h = 100; // defaults for shapes with no transform
        var xfrm = shape.ShapeProperties?.GetFirstChild<D.Transform2D>();
        if (xfrm != null)
        {
            if (xfrm.Offset is { } off)
            {
                x = Emu.ToPoints(off.X!.Value);
                y = Emu.ToPoints(off.Y!.Value);
            }
            if (xfrm.Extents is { } ext)
            {
                w = Emu.ToPoints(ext.Cx!.Value);
                h = Emu.ToPoints(ext.Cy!.Value);
            }
        }

        return new TextElement
        {
            Text = text,
            FontSize = fontSize,
            Bold = bold,
            Italic = italic,
            ColorHex = colorHex,
            X = x, Y = y,
            Width = w, Height = h
        };
    }

    // ── Pictures ─────────────────────────────────────────────────────

    /// <summary>Read a <see cref="P.Picture"/> into an <see cref="ImageElement"/>.</summary>
    private static ImageElement? ReadPicture(SlidePart slidePart, P.Picture pic)
    {
        try
        {
            var blip = pic.BlipFill?.Blip;
            if (blip?.Embed?.Value is not string embedId) return null;

            var imagePart = (ImagePart)slidePart.GetPartById(embedId);
            byte[] data;
            using (var stream = imagePart.GetStream())
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                data = ms.ToArray();
            }

            // Determine extension from content type
            string ext = imagePart.ContentType switch
            {
                "image/jpeg" => "jpg",
                "image/gif" => "gif",
                _ => "png"
            };

            // Read position
            double x = 100, y = 100, w = 400, h = 300;
            var xfrm = pic.ShapeProperties?.GetFirstChild<D.Transform2D>();
            if (xfrm != null)
            {
                if (xfrm.Offset is { } off)
                {
                    x = Emu.ToPoints(off.X!.Value);
                    y = Emu.ToPoints(off.Y!.Value);
                }
                if (xfrm.Extents is { } ext2)
                {
                    w = Emu.ToPoints(ext2.Cx!.Value);
                    h = Emu.ToPoints(ext2.Cy!.Value);
                }
            }

            return new ImageElement
            {
                Data = data,
                Extension = ext,
                X = x, Y = y,
                Width = w, Height = h
            };
        }
        catch
        {
            return null;
        }
    }

    // ── Transitions ──────────────────────────────────────────────────

    /// <summary>Map an Open XML transition element to our enum.</summary>
    private static TransitionType ReadTransition(P.Transition transition)
    {
        if (transition.GetFirstChild<P.FadeTransition>() != null)
            return TransitionType.Fade;
        if (transition.GetFirstChild<P.PushTransition>() != null)
            return TransitionType.Push;
        if (transition.GetFirstChild<P.WipeTransition>() != null)
            return TransitionType.Wipe;
        return TransitionType.None;
    }
}
