namespace slidecraft.Models;

/// <summary>
/// A text box element on a slide. Stores the text content and formatting
/// properties (font size, bold, italic, colour).
/// </summary>
public sealed class TextElement : SlideElement
{
    /// <summary>The text content of this text box.</summary>
    public string Text { get; set; } = "";

    /// <summary>Font size in points.</summary>
    public double FontSize { get; set; } = 24;

    /// <summary>Whether the text is bold.</summary>
    public bool Bold { get; set; }

    /// <summary>Whether the text is italic.</summary>
    public bool Italic { get; set; }

    /// <summary>Text colour as a six-character hex string (RRGGBB).</summary>
    public string ColorHex { get; set; } = "000000";
}
