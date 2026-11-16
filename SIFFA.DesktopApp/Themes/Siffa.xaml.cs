using System.Windows;

namespace SIFFA.DesktopApp.Themes
{
    /// <summary>Acciones de los botones de la barra de título propia (minimizar, maximizar, restaurar, cerrar).</summary>
    public partial class SiffaTheme : ResourceDictionary
    {
        public SiffaTheme()
        {
            InitializeComponent();
        }

        private static Window? Owner(object sender) => Window.GetWindow((DependencyObject)sender);

        private void OnMinimize(object sender, RoutedEventArgs e) { if (Owner(sender) is { } w) w.WindowState = WindowState.Minimized; }
        private void OnMaximize(object sender, RoutedEventArgs e) { if (Owner(sender) is { } w) w.WindowState = WindowState.Maximized; }
        private void OnRestore(object sender, RoutedEventArgs e) { if (Owner(sender) is { } w) w.WindowState = WindowState.Normal; }
        private void OnClose(object sender, RoutedEventArgs e) => Owner(sender)?.Close();
    }
}
