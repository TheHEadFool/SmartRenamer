using System;
using System.Windows.Controls;
using System.Windows.Media;
using Scout.Expeditions.Safari.Waterfall.Effects;

namespace Scout.Expeditions.Safari.Components
{
    /// <summary>
    /// Interaction logic for SafariStage.xaml
    /// </summary>
    public partial class SafariStage : UserControl
    {
        private readonly WaterfallEffects _waterfallEffects;

        public SafariStage()
        {
            InitializeComponent();

            _waterfallEffects = new WaterfallEffects(WaterfallLayer);

            CompositionTarget.Rendering += OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            _waterfallEffects.Update();
        }
    }
}