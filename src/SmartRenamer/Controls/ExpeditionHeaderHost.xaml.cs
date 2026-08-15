using Scout.Core;
using Scout.Core.Expeditions;
using SmartRenamer;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Scout.Controls;

/// <summary>
/// PURPOSE
/// Hosts the visual header supplied by the active Expedition.
///
/// WHY IT EXISTS
/// Scout's main window must not know which Expedition is active.
/// This control provides a Scout-owned presentation slot that resolves
/// the active Expedition's header from its manifest.
///
/// RESPONSIBILITIES
/// • Obtain the active Expedition manifest.
/// • Resolve the Expedition HeaderResource.
/// • Load the Expedition-provided header.
/// • Provide the Expedition manifest to the loaded header as its DataContext.
///
/// DOES NOT
/// • Contain Safari-specific presentation logic.
/// • Select the active Expedition.
/// • Execute Experts.
/// • Perform business logic.
/// • Analyze files.
///
/// ARCHITECTURE
/// Scout owns this host.
/// The active Expedition owns the actual header implementation.
/// </summary>
public partial class ExpeditionHeaderHost : UserControl
{
    public ExpeditionHeaderHost()
    {
        InitializeComponent();

        Loaded += ExpeditionHeaderHost_Loaded;
    }

    /// <summary>
    /// Loads the header defined by the active Expedition manifest.
    /// </summary>
    private void ExpeditionHeaderHost_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        LoadExpeditionHeader();
    }

    /// <summary>
    /// Resolves and loads the active Expedition's header.
    /// </summary>
    private void LoadExpeditionHeader()
    {
        if (Application.Current is not App app)
        {
            throw new InvalidOperationException(
                "Unable to access the Scout application instance.");
        }

        ExpeditionManager expeditionManager =
            app.ExpeditionManager;

        ExpeditionManifest? manifest =
            expeditionManager.CurrentManifest;

        if (manifest is null)
        {
            throw new InvalidOperationException(
                "No active Expedition manifest is available.");
        }

        if (string.IsNullOrWhiteSpace(manifest.HeaderResource))
        {
            HeaderContent.Content = null;
            return;
        }

        string? manifestPath =
            expeditionManager.CurrentManifestPath;

        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            throw new InvalidOperationException(
                "The active Expedition manifest path is not available.");
        }

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
            manifest.HeaderResource);

        resourcePath = resourcePath.Replace(
            Path.DirectorySeparatorChar,
            '/');

        Uri resourceUri = new(
            resourcePath,
            UriKind.Relative);

        object header =
            Application.LoadComponent(resourceUri);

        if (header is not FrameworkElement headerElement)
        {
            throw new InvalidOperationException(
                $"Expedition header resource '{resourcePath}' " +
                "did not produce a WPF FrameworkElement.");
        }

        // The Expedition manifest becomes the data source for
        // the Expedition-provided header.
        headerElement.DataContext = manifest;

        HeaderContent.Content = headerElement;
    }
}