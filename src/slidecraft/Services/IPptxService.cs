using slidecraft.Models;

namespace slidecraft.Services;

/// <summary>
/// Abstraction for reading and writing <see cref="PresentationModel"/>
/// instances to and from .pptx files.
/// </summary>
public interface IPptxService
{
    /// <summary>Save the model as a .pptx file at <paramref name="path"/>.</summary>
    void Save(PresentationModel model, string path);

    /// <summary>Open the .pptx file at <paramref name="path"/> and return a model.</summary>
    /// <returns>The presentation model and count of skipped elements.</returns>
    (PresentationModel Model, int SkippedCount) Open(string path);
}
