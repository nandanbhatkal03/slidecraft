namespace slidecraft.Models;

/// <summary>
/// Abstract base class for all elements that can appear on a slide.
/// Stores position and size in points on a 960 × 540 canvas.
/// </summary>
public abstract class SlideElement
{
    /// <summary>X position in points from the left edge of the slide.</summary>
    public double X { get; set; }

    /// <summary>Y position in points from the top edge of the slide.</summary>
    public double Y { get; set; }

    /// <summary>Width of the element in points.</summary>
    public double Width { get; set; }

    /// <summary>Height of the element in points.</summary>
    public double Height { get; set; }
}
