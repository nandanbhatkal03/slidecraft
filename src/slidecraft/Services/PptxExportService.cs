using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using slidecraft.Helpers;
using slidecraft.Models;
using System.IO;
using P = DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;

namespace slidecraft.Services;

/// <summary>
/// Exports a <see cref="PresentationModel"/> to a .pptx file using the
/// Open XML SDK. A blank template is generated on first use to provide
/// the required theme, slide master and slide layout parts.
/// </summary>
public sealed class PptxExportService : IPptxService
{
    private readonly string _templatePath;

    /// <summary>Creates the service, ensuring the blank template exists.</summary>
    public PptxExportService()
    {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
        Directory.CreateDirectory(dir);
        _templatePath = Path.Combine(dir, "blank.pptx");
        if (!File.Exists(_templatePath))
            CreateTemplate(_templatePath);
    }

    /// <inheritdoc />
    public void Save(PresentationModel model, string path)
    {
        File.Copy(_templatePath, path, overwrite: true);
        using var doc = PresentationDocument.Open(path, true);
        var presPart = doc.PresentationPart!;
        var layout = presPart.SlideMasterParts.First().SlideLayoutParts.First();

        // Remove any pre-existing slides in template (normally none)
        presPart.Presentation.SlideIdList?.RemoveAllChildren();

        foreach (var slide in model.Slides)
        {
            var slidePart = AddSlide(presPart, layout);
            var tree = slidePart.Slide.CommonSlideData!.ShapeTree!;
            uint id = 2;
            foreach (var el in slide.Elements)
            {
                switch (el)
                {
                    case TextElement t:
                        tree.Append(BuildTextBox(t, id++));
                        break;
                    case ImageElement i:
                        tree.Append(BuildPicture(slidePart, i, id++));
                        break;
                }
            }
            ApplyTransition(slidePart.Slide, slide.Transition);
            slidePart.Slide.Save();
        }
        presPart.Presentation.Save();
    }

    /// <inheritdoc />
    public (PresentationModel Model, int SkippedCount) Open(string path)
    {
        // Delegation to PptxImportService — this interface method is
        // present for symmetry; see PptxImportService.Open().
        throw new NotSupportedException("Use PptxImportService for opening.");
    }

    // ── Template generation ──────────────────────────────────────────

    /// <summary>Create a minimal valid .pptx with theme, master and layout but no slides.</summary>
    private static void CreateTemplate(string path)
    {
        using var doc = PresentationDocument.Create(path, PresentationDocumentType.Presentation);

        // Presentation part
        var presPart = doc.AddPresentationPart();
        presPart.Presentation = new P.Presentation(
            new P.SlideMasterIdList(),
            new P.SlideIdList(),
            new P.SlideSize { Cx = 12192000, Cy = 6858000, Type = P.SlideSizeValues.Custom },
            new P.NotesSize { Cx = 6858000, Cy = 9144000 });

        // Theme
        var themePart = presPart.AddNewPart<ThemePart>();
        themePart.Theme = CreateTheme();
        themePart.Theme.Save();

        // Slide master
        var masterPart = presPart.AddNewPart<SlideMasterPart>();
        masterPart.AddPart(themePart);

        // Slide layout
        var layoutPart = masterPart.AddNewPart<SlideLayoutPart>();
        layoutPart.SlideLayout = new P.SlideLayout(
            new P.CommonSlideData(new P.ShapeTree(
                new P.NonVisualGroupShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 1U, Name = "" },
                    new P.NonVisualGroupShapeDrawingProperties(),
                    new P.ApplicationNonVisualDrawingProperties()),
                new P.GroupShapeProperties(new D.TransformGroup()))),
            new P.ColorMapOverride(new D.MasterColorMapping()))
        { Type = P.SlideLayoutValues.Blank };
        layoutPart.SlideLayout.Save();

        // Link layout back to master
        layoutPart.AddPart(masterPart);

        // Slide master content
        masterPart.SlideMaster = new P.SlideMaster(
            new P.CommonSlideData(new P.ShapeTree(
                new P.NonVisualGroupShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 1U, Name = "" },
                    new P.NonVisualGroupShapeDrawingProperties(),
                    new P.ApplicationNonVisualDrawingProperties()),
                new P.GroupShapeProperties(new D.TransformGroup()))),
            new P.ColorMap
            {
                Background1 = D.ColorSchemeIndexValues.Light1,
                Text1 = D.ColorSchemeIndexValues.Dark1,
                Background2 = D.ColorSchemeIndexValues.Light2,
                Text2 = D.ColorSchemeIndexValues.Dark2,
                Accent1 = D.ColorSchemeIndexValues.Accent1,
                Accent2 = D.ColorSchemeIndexValues.Accent2,
                Accent3 = D.ColorSchemeIndexValues.Accent3,
                Accent4 = D.ColorSchemeIndexValues.Accent4,
                Accent5 = D.ColorSchemeIndexValues.Accent5,
                Accent6 = D.ColorSchemeIndexValues.Accent6,
                Hyperlink = D.ColorSchemeIndexValues.Hyperlink,
                FollowedHyperlink = D.ColorSchemeIndexValues.FollowedHyperlink
            },
            new P.SlideLayoutIdList(
                new P.SlideLayoutId
                {
                    Id = 2147483649U,
                    RelationshipId = masterPart.GetIdOfPart(layoutPart)
                }));
        masterPart.SlideMaster.Save();

        // Register master in presentation
        presPart.Presentation.SlideMasterIdList!.Append(
            new P.SlideMasterId
            {
                Id = 2147483648U,
                RelationshipId = presPart.GetIdOfPart(masterPart)
            });

        presPart.Presentation.Save();
    }

    /// <summary>Create a minimal Office-compatible theme.</summary>
    private static D.Theme CreateTheme()
    {
        return new D.Theme(
            new D.ThemeElements(
                new D.ColorScheme(
                    new D.Dark1Color(new D.SystemColor { Val = D.SystemColorValues.WindowText, LastColor = "000000" }),
                    new D.Light1Color(new D.SystemColor { Val = D.SystemColorValues.Window, LastColor = "FFFFFF" }),
                    new D.Dark2Color(new D.RgbColorModelHex { Val = "44546A" }),
                    new D.Light2Color(new D.RgbColorModelHex { Val = "E7E6E6" }),
                    new D.Accent1Color(new D.RgbColorModelHex { Val = "4472C4" }),
                    new D.Accent2Color(new D.RgbColorModelHex { Val = "ED7D31" }),
                    new D.Accent3Color(new D.RgbColorModelHex { Val = "A5A5A5" }),
                    new D.Accent4Color(new D.RgbColorModelHex { Val = "FFC000" }),
                    new D.Accent5Color(new D.RgbColorModelHex { Val = "5B9BD5" }),
                    new D.Accent6Color(new D.RgbColorModelHex { Val = "70AD47" }),
                    new D.Hyperlink(new D.RgbColorModelHex { Val = "0563C1" }),
                    new D.FollowedHyperlinkColor(new D.RgbColorModelHex { Val = "954F72" })
                )
                { Name = "Office" },
                new D.FontScheme(
                    new D.MajorFont(
                        new D.LatinFont { Typeface = "Calibri Light" },
                        new D.EastAsianFont { Typeface = "" },
                        new D.ComplexScriptFont { Typeface = "" }),
                    new D.MinorFont(
                        new D.LatinFont { Typeface = "Calibri" },
                        new D.EastAsianFont { Typeface = "" },
                        new D.ComplexScriptFont { Typeface = "" })
                )
                { Name = "Office" },
                new D.FormatScheme(
                    new D.FillStyleList(
                        new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor }),
                        new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor }),
                        new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })),
                    new D.LineStyleList(
                        new D.Outline(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })) { Width = 6350 },
                        new D.Outline(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })) { Width = 12700 },
                        new D.Outline(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })) { Width = 19050 }),
                    new D.EffectStyleList(
                        new D.EffectStyle(new D.EffectList()),
                        new D.EffectStyle(new D.EffectList()),
                        new D.EffectStyle(new D.EffectList())),
                    new D.BackgroundFillStyleList(
                        new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor }),
                        new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor }),
                        new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor }))
                )
                { Name = "Office" }
            ))
        { Name = "Office Theme" };
    }

    // ── Slide building ───────────────────────────────────────────────

    /// <summary>Append a new slide to the presentation, linked to the given layout.</summary>
    private static SlidePart AddSlide(PresentationPart presPart, SlideLayoutPart layout)
    {
        var slidePart = presPart.AddNewPart<SlidePart>();
        slidePart.Slide = new P.Slide(
            new P.CommonSlideData(new P.ShapeTree(
                new P.NonVisualGroupShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 1U, Name = "" },
                    new P.NonVisualGroupShapeDrawingProperties(),
                    new P.ApplicationNonVisualDrawingProperties()),
                new P.GroupShapeProperties(new D.TransformGroup()))),
            new P.ColorMapOverride(new D.MasterColorMapping()));
        slidePart.AddPart(layout);

        var pres = presPart.Presentation;
        var list = pres.SlideIdList;
        if (list is null)
            list = pres.InsertAfter(new P.SlideIdList(), pres.SlideMasterIdList!);
        uint next = list.Elements<P.SlideId>().Select(s => s.Id!.Value).DefaultIfEmpty(255U).Max() + 1;
        list.Append(new P.SlideId { Id = next, RelationshipId = presPart.GetIdOfPart(slidePart) });
        return slidePart;
    }

    /// <summary>Build an Open XML text box shape from a <see cref="TextElement"/>.</summary>
    private static P.Shape BuildTextBox(TextElement t, uint id)
    {
        // Split on newlines and emit one paragraph per line
        var paragraphs = new List<D.Paragraph>();
        var lines = t.Text.Split('\n');
        foreach (var line in lines)
        {
            var runProps = new D.RunProperties
            {
                Language = "en-US",
                FontSize = (int)(t.FontSize * 100),
                Bold = t.Bold,
                Italic = t.Italic
            };
            runProps.Append(new D.SolidFill(new D.RgbColorModelHex { Val = t.ColorHex }));
            runProps.Append(new D.ComplexScriptFont { Typeface = "Nirmala UI" });
            paragraphs.Add(new D.Paragraph(new D.Run(runProps, new D.Text(line))));
        }

        var textBody = new P.TextBody(
            new D.BodyProperties { Wrap = D.TextWrappingValues.Square },
            new D.ListStyle());
        foreach (var p in paragraphs) textBody.Append(p);

        return new P.Shape(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties { Id = id, Name = $"TextBox {id}" },
                new P.NonVisualShapeDrawingProperties { TextBox = true },
                new P.ApplicationNonVisualDrawingProperties()),
            new P.ShapeProperties(
                new D.Transform2D(
                    new D.Offset { X = Emu.ToEmu(t.X), Y = Emu.ToEmu(t.Y) },
                    new D.Extents { Cx = Emu.ToEmu(t.Width), Cy = Emu.ToEmu(t.Height) }),
                new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle }),
            textBody);
    }

    /// <summary>Build an Open XML picture shape from an <see cref="ImageElement"/>.</summary>
    private static P.Picture BuildPicture(SlidePart slidePart, ImageElement img, uint id)
    {
        var type = img.Extension.ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => ImagePartType.Jpeg,
            "gif" => ImagePartType.Gif,
            _ => ImagePartType.Png
        };
        var imgPart = slidePart.AddImagePart(type);
        using (var ms = new MemoryStream(img.Data)) imgPart.FeedData(ms);

        return new P.Picture(
            new P.NonVisualPictureProperties(
                new P.NonVisualDrawingProperties { Id = id, Name = $"Picture {id}" },
                new P.NonVisualPictureDrawingProperties(new D.PictureLocks { NoChangeAspect = true }),
                new P.ApplicationNonVisualDrawingProperties()),
            new P.BlipFill(
                new D.Blip { Embed = slidePart.GetIdOfPart(imgPart) },
                new D.Stretch(new D.FillRectangle())),
            new P.ShapeProperties(
                new D.Transform2D(
                    new D.Offset { X = Emu.ToEmu(img.X), Y = Emu.ToEmu(img.Y) },
                    new D.Extents { Cx = Emu.ToEmu(img.Width), Cy = Emu.ToEmu(img.Height) }),
                new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle }));
    }

    /// <summary>Append a transition element to a slide if the type is not None.</summary>
    private static void ApplyTransition(P.Slide slide, TransitionType type)
    {
        if (type == TransitionType.None) return;
        OpenXmlElement effect = type switch
        {
            TransitionType.Fade => new P.FadeTransition(),
            TransitionType.Push => new P.PushTransition { Direction = P.TransitionSlideDirectionValues.Left },
            _ => new P.WipeTransition { Direction = P.TransitionSlideDirectionValues.Right }
        };
        slide.Append(new P.Transition(effect) { Speed = P.TransitionSpeedValues.Medium });
    }
}
