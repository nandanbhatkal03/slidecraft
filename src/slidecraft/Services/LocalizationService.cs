using System.Windows;

namespace slidecraft.Services;

/// <summary>
/// Phase-1 stub of the localization service. Retrieves string resources
/// from the application's merged resource dictionaries.
/// Full language-switching logic is added in Phase 2.
/// </summary>
public sealed class LocalizationService
{
    /// <summary>Singleton instance.</summary>
    public static LocalizationService Instance { get; } = new();

    /// <summary>Current culture code ("en" or "kn").</summary>
    public string CurrentCulture { get; private set; } = "en";

    /// <summary>Fired after the UI language changes.</summary>
    public event Action? LanguageChanged;

    /// <summary>
    /// Retrieves a localised string by resource key. Returns the key itself
    /// if the resource is not found.
    /// </summary>
    public string Get(string key) =>
        Application.Current.TryFindResource(key) as string ?? key;
}
