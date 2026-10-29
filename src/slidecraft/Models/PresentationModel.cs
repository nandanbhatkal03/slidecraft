namespace slidecraft.Models;

/// <summary>
/// Root model for an entire presentation. Contains an ordered list of
/// slides and an optional file path for save/open operations.
/// </summary>
public sealed class PresentationModel
{
    /// <summary>The slides in this presentation, in display order.</summary>
    public List<SlideModel> Slides { get; set; } = new();

    /// <summary>
    /// The file path this presentation was last saved to or opened from.
    /// <c>null</c> if the presentation has never been saved.
    /// </summary>
    public string? FilePath { get; set; }
}
