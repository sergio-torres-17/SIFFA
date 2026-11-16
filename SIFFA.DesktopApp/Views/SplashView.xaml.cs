using System.Windows;

namespace SIFFA.DesktopApp.Views
{
    /// <summary>Pantalla de arranque: muestra la marca y en qué paso va la verificación inicial.</summary>
    public partial class SplashView : Window
    {
        public SplashView()
        {
            InitializeComponent();
        }

        public void SetStatus(string text) => StatusText.Text = text;
    }
}
