using slidecraft.Helpers;
using slidecraft.Models;

namespace slidecraft.Services;

/// <summary>
/// Creates slides pre-populated with placeholder <see cref="TextElement"/>s
/// according to a named layout.
/// </summary>
public static class LayoutFactory
{
    /// <summary>
    /// Populate <paramref name="slide"/> with placeholder elements matching
    /// the requested layout.
    /// </summary>
    /// <param name="slide">The slide model to populate.</param>
    /// <param name="layoutKey">
    /// One of <c>"Title"</c>, <c>"TitleContent"</c>, or <c>"Blank"</c>.
    /// </param>
    public static void Apply(SlideModel slide, string? layoutKey)
    {
        var loc = LocalizationService.Instance;

        switch (layoutKey)
        {
            case "Title":
                slide.Elements.Add(new TextElement
                {
                    Text = loc.Get("Str_ClickToAddTitle"),
                    X = 60, Y = 160,
                    Width = 840, Height = 80,
                    FontSize = 44,
                    Bold = true
                });
                slide.Elements.Add(new TextElement
                {
                    Text = loc.Get("Str_ClickToAddText"),
                    X = 160, Y = 300,
                    Width = 640, Height = 60,
                    FontSize = 24
                });
                break;

            case "TitleContent":
                slide.Elements.Add(new TextElement
                {
                    Text = loc.Get("Str_ClickToAddTitle"),
                    X = 40, Y = 20,
                    Width = 880, Height = 60,
                    FontSize = 36,
                    Bold = true
                });
                slide.Elements.Add(new TextElement
                {
                    Text = loc.Get("Str_ClickToAddText"),
                    X = 40, Y = 100,
                    Width = 880, Height = 400,
                    FontSize = 24
                });
                break;

            case "Blank":
            default:
                // No placeholders
                break;
        }
    }
}
