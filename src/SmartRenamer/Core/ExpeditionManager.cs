using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Scout.Core.Expeditions;

namespace Scout.Core;

/// <summary>
/// PURPOSE
/// Initializes and loads the active Scout Expedition.
///
/// WHY IT EXISTS
/// Scout separates appearance from behavior. ExpeditionManager is the single
/// component responsible for loading the resources that define an Expedition
/// and making the active Expedition manifest available to Scout.
///
/// RESPONSIBILITIES
/// • Locate the active Expedition manifest.
/// • Deserialize the manifest.
/// • Retain the active Expedition manifest.
/// • Determine the Expedition root directory.
/// • Resolve Expedition resources relative to that Expedition.
/// • Load the Expedition ResourceDictionary.
/// • Merge Expedition resources into the application.
///
/// DOES NOT
/// • Load Experts.
/// • Execute plugin code.
/// • Modify business logic.
/// • Analyze files.
///
/// DEPENDENCIES
/// • ExpeditionManifest
/// • System.Text.Json
/// • WPF ResourceDictionary
///
/// PACKAGE
/// P002 - Expedition Startup
///
/// ADR
/// ADR-011 - Expedition Plugin Architecture
/// </summary>
public sealed class ExpeditionManager
{
    /// <summary>
    /// Gets the manifest for the currently active Expedition.
    ///
    /// The manifest is retained after initialization so other parts of Scout
    /// can obtain Expedition-provided information without knowing which
    /// Expedition is active.
    /// </summary>
    public ExpeditionManifest? CurrentManifest { get; private set; }

    /// <summary>
    /// Gets the path to the manifest for the currently active Expedition.
    ///
    /// This allows Scout to understand where the active Expedition lives
    /// without hard-coding Safari-specific resource paths elsewhere.
    /// </summary>
    public string? CurrentManifestPath { get; private set; }

    /// <summary>
    /// Initializes the current Expedition.
    /// </summary>
    public void Initialize()
    {
        string manifestPath = GetCurrentManifestPath();

        ExpeditionManifest? manifest = LoadManifest(manifestPath);

        if (manifest is null)
        {
            throw new InvalidOperationException(
                $"Unable to load Expedition manifest '{manifestPath}'.");
        }

        CurrentManifestPath = manifestPath;
        CurrentManifest = manifest;

        LoadTheme(manifest, manifestPath);
    }

    /// <summary>
    /// Returns the path to the currently selected Expedition.
    ///
    /// P002 currently uses Safari as the default Expedition.
    /// Later this selection will come from Scout Settings or an
    /// Expedition selection service.
    /// </summary>
    private static string GetCurrentManifestPath()
    {
        // P002
        // Safari is the only Expedition currently available.
        // Later this will come from Scout Settings.

        return "Expeditions/Safari/Expedition.json";
    }

    /// <summary>
    /// Loads an Expedition manifest from disk.
    /// </summary>
    private static ExpeditionManifest? LoadManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
            return null;

        string json = File.ReadAllText(manifestPath);

        return JsonSerializer.Deserialize<ExpeditionManifest>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
    }

    /// <summary>
    /// Loads the Expedition theme into the application.
    ///
    /// The ThemeResource path declared by the manifest is relative to the
    /// Expedition folder, not the Scout application root.
    ///
    /// Example:
    ///
    /// Expedition manifest:
    ///     Expeditions/Safari/Expedition.json
    ///
    /// Manifest resource:
    ///     Foundation/SafariTheme.xaml
    ///
    /// Resolved resource:
    ///     Expeditions/Safari/Foundation/SafariTheme.xaml
    /// </summary>
    private static void LoadTheme(
        ExpeditionManifest manifest,
        string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifest.ThemeResource))
            return;

        string? expeditionDirectory =
            Path.GetDirectoryName(manifestPath);

        if (string.IsNullOrWhiteSpace(expeditionDirectory))
        {
            throw new InvalidOperationException(
                $"Unable to determine the Expedition directory from " +
                $"manifest path '{manifestPath}'.");
        }

        string resourcePath = Path.Combine(
            expeditionDirectory,
            manifest.ThemeResource);

        resourcePath = resourcePath.Replace(
            Path.DirectorySeparatorChar,
            '/');

        var dictionary = new ResourceDictionary
        {
            Source = new Uri(
                resourcePath,
                UriKind.Relative)
        };

        Application.Current.Resources
            .MergedDictionaries
            .Add(dictionary);
    }
}