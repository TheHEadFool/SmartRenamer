using System.Windows.Controls;

namespace Scout.Expeditions.Safari.Camp
{
    public partial class CampFire : UserControl
    {
        private readonly CampFireEffects _effects;

        public CampFire()
        {
            InitializeComponent();

            _effects = new CampFireEffects(SmokeLayer);
        }
    }
}