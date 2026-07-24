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
/// component responsible for loading the visual resources that define an
/// Expedition.
///
/// RESPONSIBILITIES
/// • Locate the active Expedition manifest.
/// • Deserialize the manifest.
/// • Load the Expedition ResourceDictionary.
/// • Merge Expedition resources into the application.
///
/// DOES NOT
/// • Load Experts.
/// • Execute plugin code.
/// • Modify business logic.
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

        LoadTheme(manifest);
    }

    /// <summary>
    /// Returns the path to the currently selected Expedition.
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
    /// </summary>
    private static void LoadTheme(ExpeditionManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.ThemeResource))
            return;

        var dictionary = new ResourceDictionary
        {
            Source = new Uri(
                manifest.ThemeResource,
                UriKind.Relative)
        };

        Application.Current.Resources.MergedDictionaries.Add(dictionary);
    }
}