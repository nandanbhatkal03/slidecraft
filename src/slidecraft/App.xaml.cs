using System.Windows;

namespace slidecraft;

/// <summary>
/// Application entry point. Loads the default (English) language dictionary
/// on startup.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Called when the application starts. Ensures the English string
    /// dictionary is loaded as the default language.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
    }
}
