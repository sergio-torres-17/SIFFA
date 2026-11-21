using System.Windows;
using System.Windows.Controls;
using SIFFA.Core.Services.AppregistryService;
using SIFFA.DesktopApp.Services;

namespace SIFFA.DesktopApp.Views
{
    /// <summary>Pide la clave de licencia y la valida contra Appregistry. DialogResult = true si quedó válida.</summary>
    public class LicenseView : Window
    {
        private readonly LicenseManager _licenses;
        private readonly TextBox _key = new() { Margin = new Thickness(0, 6, 0, 0), Padding = new Thickness(6), FontSize = 15 };
        private readonly TextBlock _message = new() { Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.Wrap };
        private readonly Button _ok = new() { Content = "Validar", Padding = new Thickness(18, 6, 18, 6), IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
        private readonly Button _cancel = new() { Content = "Salir", Padding = new Thickness(18, 6, 18, 6), IsCancel = true };

        public LicenseView(LicenseManager licenses, string? message = null)
        {
            _licenses = licenses;
            Title = "SIFFA - Licencia";
            Width = 460;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
            buttons.Children.Add(_ok);
            buttons.Children.Add(_cancel);

            var panel = new StackPanel { Margin = new Thickness(22) };
            panel.Children.Add(new TextBlock { Text = "Licencia de SIFFA", FontSize = 20, FontWeight = FontWeights.SemiBold });
            panel.Children.Add(new TextBlock
            {
                Text = "Escribe la clave de licencia de este equipo. Se valida contra Appregistry y se recuerda para las siguientes veces.",
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0),
            });
            panel.Children.Add(_key);
            _message.Text = message ?? "";
            _message.Foreground = System.Windows.Media.Brushes.Firebrick;
            panel.Children.Add(_message);
            panel.Children.Add(buttons);
            Content = panel;

            _key.Text = licenses.SavedKey ?? "";
            Loaded += (_, _) => _key.Focus();
            _ok.Click += async (_, _) => await ValidateAsync();
        }

        private async Task ValidateAsync()
        {
            var key = _key.Text.Trim();
            if (key.Length == 0) { _message.Text = "Escribe la clave de licencia."; return; }

            _ok.IsEnabled = _key.IsEnabled = false;
            _message.Foreground = System.Windows.Media.Brushes.Gray;
            _message.Text = "Validando…";
            var result = await _licenses.ValidateAsync(key);
            _ok.IsEnabled = _key.IsEnabled = true;

            if (result.State == LicenseState.Valid)
            {
                DialogResult = true;
                return;
            }
            _message.Foreground = System.Windows.Media.Brushes.Firebrick;
            _message.Text = result.Message;
        }
    }
}
