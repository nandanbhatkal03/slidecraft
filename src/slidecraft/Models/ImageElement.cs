namespace slidecraft.Models;

/// <summary>
/// An image element on a slide. Stores the raw image bytes and the
/// file extension so the correct image codec can be used during export.
/// </summary>
public sealed class ImageElement : SlideElement
{
    /// <summary>Raw image data (PNG, JPEG, or GIF bytes).</summary>
    public byte[] Data { get; set; } = [];

    /// <summary>File extension without the dot (png, jpg, gif).</summary>
    public string Extension { get; set; } = "png";
}
