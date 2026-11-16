using Microsoft.Extensions.DependencyInjection;
using SIFFA.Core.Models.Custom.Model;
using SIFFA.Core.Models.Custom.Request.AccessControl;
using SIFFA.Core.Models.Custom.Response;
using SIFFA.Core.Services.AccessControlService;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SIFFA.DesktopApp.Views
{
    /// <summary>Captura del código (lector de barras) o búsqueda por nombre/correo para registrar un ingreso o egreso.</summary>
    public partial class AccessScanView : Window
    {
        private readonly IServiceProvider _services;
        private readonly AccessType _type;
        private bool _busy;

        public AccessScanView(IServiceProvider services, AccessType type)
        {
            InitializeComponent();
            _services = services;
            _type = type;
            TxtTitle.Text = type == AccessType.Entry ? "Registrar ingreso" : "Registrar egreso";
            Loaded += (_, _) => TxtTerm.Focus();
        }

        /// <summary>Respuesta del registro cuando se completó con éxito.</summary>
        public AddTraceResponse? Response { get; private set; }

        private sealed record Match(EmployeeSearchResult Item)
        {
            public string FullName => Item.FullName;
            public string Detail => $"No. {Item.EmployeeNumber} · {Item.Area} · {Item.Email}";
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtStatus is null) return;
            TxtStatus.Visibility = Visibility.Collapsed;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Enter) { e.Handled = true; Submit(); }
        }

        private void Submit()
        {
            if (_busy) return;
            var term = TxtTerm.Text.Trim();
            if (term.Length == 0) return;

            MatchesPanel.Visibility = Visibility.Collapsed;
            _busy = true;
            try
            {
                using var scope = _services.CreateScope();
                var found = scope.ServiceProvider.GetRequiredService<IAccessControlService>().SearchEmployees(term);

                if (found.Count == 0)
                {
                    ShowStatus("No se encontró ningún empleado con ese dato.", ok: false);
                    Reset();
                }
                else if (found.Count == 1)
                {
                    Register(found[0].EmployeeNumber);
                }
                else
                {
                    Matches.ItemsSource = found.Select(f => new Match(f)).ToList();
                    MatchesPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"No se pudo buscar: {ex.GetBaseException().Message}", ok: false);
            }
            finally
            {
                _busy = false;
            }
        }

        private void OnPick(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).Tag is Match match)
                Register(match.Item.EmployeeNumber);
        }

        private void Register(string employeeNumber)
        {
            using var scope = _services.CreateScope();
            var response = scope.ServiceProvider.GetRequiredService<IAccessControlService>().AddTrace(
                new InsertTraceAccessControl { IdentifierNumber = employeeNumber, AccessType = _type });

            if (response.status)
            {
                Response = response;
                DialogResult = true;
                return;
            }

            // Falló (por ejemplo, movimiento repetido): se avisa y se deja listo para el siguiente código.
            ShowStatus(response.message, ok: false);
            MatchesPanel.Visibility = Visibility.Collapsed;
            Reset();
        }

        private void Reset()
        {
            TxtTerm.Clear();
            TxtTerm.Focus();
        }

        private void ShowStatus(string message, bool ok)
        {
            TxtStatus.Text = message;
            TxtStatus.Foreground = (Brush)FindResource(ok ? "BrushSuccess" : "BrushError");
            TxtStatus.Visibility = Visibility.Visible;
        }

        private void OnClose(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
