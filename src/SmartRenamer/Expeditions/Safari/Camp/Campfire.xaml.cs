using System.Windows.Controls;

namespace Scout.Expeditions.Safari.Camp
{
    public partial class CampFire : UserControl
    {
        private readonly EmberSystem _embers;

        public CampFire()
        {
            InitializeComponent();

            _embers = new EmberSystem(SmokeLayer);
            _embers.Start();
        }
    }
}