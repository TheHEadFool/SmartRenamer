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
/// • Continue normal WPF startup.
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
    protected override void OnStartup(StartupEventArgs e)
    {
        var expeditionManager = new ExpeditionManager();

        expeditionManager.Initialize();

        base.OnStartup(e);
    }
}