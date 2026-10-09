namespace slidecraft.Models;

/// <summary>
/// Available slide transition animations for export to .pptx.
/// </summary>
public enum TransitionType
{
    /// <summary>No transition effect.</summary>
    None,

    /// <summary>Fade in/out transition.</summary>
    Fade,

    /// <summary>Push transition (slide pushes previous slide off-screen).</summary>
    Push,

    /// <summary>Wipe transition (new slide wipes over the previous).</summary>
    Wipe
}
