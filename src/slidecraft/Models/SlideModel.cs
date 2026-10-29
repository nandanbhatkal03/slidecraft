namespace slidecraft.Models;

/// <summary>
/// Represents a single slide in a presentation. Contains a list of
/// <see cref="SlideElement"/> objects and an optional transition effect.
/// </summary>
public sealed class SlideModel
{
    /// <summary>The visual elements (text boxes, images) on this slide.</summary>
    public List<SlideElement> Elements { get; set; } = new();

    /// <summary>The transition animation applied when entering this slide.</summary>
    public TransitionType Transition { get; set; } = TransitionType.None;
}
