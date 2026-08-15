using Scout.Core;
using System.Windows;

namespace SmartRenamer;

/// <summary>
/// PURPOSE
/// Starts the Scout application.
///
/// WHY IT EXISTS
/// App coordinates application startup. During startup it initializes the
/// active Expedition before allowing the WPF application to continue.
///
/// RESPONSIBILITIES
/// • Initialize global application services.
/// • Initialize the active Expedition.
/// • Expose the active Expedition to the application.
///
/// DOES NOT
/// • Select the active Expedition.
/// • Load Experts.
/// • Perform business logic.
/// • Analyze files.
///
/// DEPENDENCIES
/// • ExpeditionManager
///
/// PACKAGE
/// P002 - Expedition Startup
///
/// ADR
/// ADR-011 - Expedition Plugin Architecture
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Provides access to the Expedition Manager initialized during startup.
    ///
    /// This allows the rest of the application to obtain information about
    /// the active Expedition without directly knowing which Expedition is
    /// currently loaded.
    /// </summary>
    public ExpeditionManager ExpeditionManager { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        ExpeditionManager.Initialize();

        base.OnStartup(e);
    }
}