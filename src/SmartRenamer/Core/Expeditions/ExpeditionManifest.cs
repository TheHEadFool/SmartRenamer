namespace Scout.Core.Expeditions;

public sealed class ExpeditionManifest
{
    /// <summary>
    /// Version of the Expedition manifest schema.
    /// Allows Scout to evolve while remaining backward compatible.
    /// </summary>
    public int ManifestVersion { get; init; } = 1;

    /// <summary>
    /// Friendly name shown in the Expedition Manager.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Expedition author.
    /// </summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>
    /// Expedition version.
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Description displayed to the user.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Supports Scout dark mode.
    /// </summary>
    public bool SupportsDarkMode { get; init; }

    /// <summary>
    /// Relative path to the primary ResourceDictionary.
    /// </summary>
    public string ThemeResource { get; init; } = string.Empty;

    /// <summary>
    /// Optional header template.
    /// </summary>
    public string HeaderResource { get; init; } = string.Empty;

    /// <summary>
    /// Name of the progress visualization control.
    /// </summary>
    public string ProgressControl { get; init; } = string.Empty;
}