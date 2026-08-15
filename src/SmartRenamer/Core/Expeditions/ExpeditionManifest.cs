namespace Scout.Core.Expeditions;

/// <summary>
/// =========================================================================
/// ExpeditionManifest
/// =========================================================================
///
/// PURPOSE
/// Defines the declarative contract between Scout and an Expedition.
///
/// WHY IT EXISTS
/// Scout is designed to provide a reusable application shell while allowing
/// individual Expeditions to supply their own identity, presentation, and
/// resources.
///
/// The manifest is the Expedition's description of what it provides to Scout.
/// Scout reads the manifest without needing to know which Expedition is
/// currently active.
///
/// ARCHITECTURAL PRINCIPLE
///
///     Scout Shell
///          │
///          ▼
///     ExpeditionManager
///          │
///          ▼
///     ExpeditionManifest
///          │
///          ├── Identity
///          ├── Presentation
///          ├── Resources
///          └── Expedition capabilities
///
/// The manifest contains configuration and descriptive information.
/// It does NOT contain business logic.
///
/// DOES NOT
/// • Execute Experts.
/// • Analyze files.
/// • Contain workflow logic.
/// • Execute plugin code.
/// • Define Scout business rules.
///
/// PACKAGE
/// P002 - Expedition Startup
///
/// ADR
/// ADR-011 - Expedition Plugin Architecture
/// =========================================================================
/// </summary>
public sealed class ExpeditionManifest
{
    /// <summary>
    /// Version of the Expedition manifest schema.
    ///
    /// This describes the structure of the manifest itself rather than the
    /// version of the Expedition.
    ///
    /// Allows Scout to evolve the manifest while maintaining compatibility
    /// with earlier Expedition definitions.
    /// </summary>
    public int ManifestVersion { get; init; } = 1;

    /// <summary>
    /// Friendly name of the Expedition.
    ///
    /// This is the human-readable identity of the Expedition and may be
    /// displayed by Scout in Expedition-related UI.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Author or creator of the Expedition.
    /// </summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>
    /// Version of the Expedition itself.
    ///
    /// This is separate from ManifestVersion, which identifies the schema
    /// used by the manifest.
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable description of the Expedition.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Indicates whether the Expedition provides support for Scout dark mode.
    /// </summary>
    public bool SupportsDarkMode { get; init; }

    /// <summary>
    /// Relative path to the primary ResourceDictionary supplied by the
    /// Expedition.
    ///
    /// The path is resolved relative to the Expedition directory rather than
    /// the Scout application root.
    ///
    /// Example:
    ///
    ///     Foundation/SafariTheme.xaml
    ///
    /// This allows each Expedition to provide its own visual resources
    /// without hard-coding those resources into Scout.
    /// </summary>
    public string ThemeResource { get; init; } = string.Empty;

    /// <summary>
    /// Relative path to the visual header supplied by the Expedition.
    ///
    /// The path is resolved relative to the Expedition directory.
    ///
    /// Scout provides the generic header host. The active Expedition provides
    /// the actual visual implementation.
    ///
    /// This prevents Scout's MainWindow from needing to know about specific
    /// Expeditions such as Safari.
    ///
    /// Example:
    ///
    ///     Components/SafariHeader.xaml
    /// </summary>
    public string HeaderResource { get; init; } = string.Empty;

    /// <summary>
    /// Title supplied to the Expedition-provided header.
    ///
    /// This allows the Expedition to control its own presentation language
    /// rather than requiring Scout's MainWindow to contain Expedition-specific
    /// text.
    ///
    /// A future Expedition may provide a completely different title without
    /// requiring changes to Scout's application shell.
    /// </summary>
    public string HeaderTitle { get; init; } = string.Empty;

    /// <summary>
    /// Subtitle supplied to the Expedition-provided header.
    ///
    /// This allows the Expedition to provide its own descriptive language and
    /// keeps Expedition-specific wording out of Scout's generic shell.
    ///
    /// A future Expedition may provide a completely different subtitle
    /// without requiring changes to MainWindow.xaml.
    /// </summary>
    public string HeaderSubtitle { get; init; } = string.Empty;

    /// <summary>
    /// Name of the progress visualization control supplied by the Expedition.
    ///
    /// This establishes a manifest-level extension point for Expedition
    /// progress presentation.
    /// </summary>
    public string ProgressControl { get; init; } = string.Empty;
}