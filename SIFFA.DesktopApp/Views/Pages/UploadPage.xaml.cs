using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SIFFA.Core.Models;
using SIFFA.Core.Models.Custom.Request.Employeees;
using SIFFA.Core.Services.EmployeeService;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SIFFA.DesktopApp.Views.Pages
{
    /// <summary>Asistente de 4 pasos: Excel de empleados → rubro → cantidades de adicionales → resumen y registro.</summary>
    public partial class UploadPage : UserControl
    {
        private readonly IServiceProvider _services;

        private int _step;
        private List<EmployeeImportRow> _rows = new();
        private List<string> _warnings = new();
        private List<string> _areas = new();
        private List<string> _types = new();
        private List<AddType> _addTypes = new();
        private AddAssignmentMode? _mode;
        private readonly Dictionary<string, Dictionary<Guid, int>> _quantities = new(StringComparer.OrdinalIgnoreCase);

        private bool _reading;
        private bool _saving;
        private bool _finished;
        private readonly string[] _titles = { "Archivo de empleados", "Rubro de asignación", "Asignación de adicionales", "Resumen" };

        public UploadPage(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;

            SampleGrid.ItemsSource = new[] { new { A = "Interno", B = "1001", C = "Ana", D = "López Ruiz", E = "Ventas", F = "ana@empresa.com" } };
            SampleGrid.Columns.Add(new DataGridTextColumn { Header = "A · Tipo de empleado", Binding = new System.Windows.Data.Binding("A"), Width = new DataGridLength(1.5, DataGridLengthUnitType.Star) });
            SampleGrid.Columns.Add(new DataGridTextColumn { Header = "B · Número", Binding = new System.Windows.Data.Binding("B"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            SampleGrid.Columns.Add(new DataGridTextColumn { Header = "C · Nombre", Binding = new System.Windows.Data.Binding("C"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            SampleGrid.Columns.Add(new DataGridTextColumn { Header = "D · Apellidos", Binding = new System.Windows.Data.Binding("D"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            SampleGrid.Columns.Add(new DataGridTextColumn { Header = "E · Área", Binding = new System.Windows.Data.Binding("E"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            SampleGrid.Columns.Add(new DataGridTextColumn { Header = "F · Correo", Binding = new System.Windows.Data.Binding("F"), Width = new DataGridLength(1.5, DataGridLengthUnitType.Star) });

            Loaded += async (_, _) => await LoadAddTypesAsync();
            Refresh();
        }

        private async Task LoadAddTypesAsync()
        {
            try
            {
                using var scope = _services.CreateScope();
                _addTypes = await scope.ServiceProvider.GetRequiredService<IEmployeeService>().GetAddTypesAsync();
            }
            catch (Exception ex)
            {
                ShowReadResult($"No se pudo cargar el catálogo de adicionales: {ex.GetBaseException().Message}", ok: false);
            }
        }

        // ---------- datos derivados ----------

        private IEnumerable<string> CurrentKeys => _mode == AddAssignmentMode.ByEmployeeType ? _types : _areas;

        private int CountFor(string key) => _mode == AddAssignmentMode.ByEmployeeType
            ? _rows.Count(r => string.Equals(r.EmployeeTypeName, key, StringComparison.OrdinalIgnoreCase))
            : _rows.Count(r => string.Equals(r.AreaName, key, StringComparison.OrdinalIgnoreCase));

        private int GetQty(string key, Guid addTypeId) =>
            _quantities.TryGetValue(key, out var byType) && byType.TryGetValue(addTypeId, out var q) ? q : 0;

        private void SetQty(string key, Guid addTypeId, int value)
        {
            if (!_quantities.TryGetValue(key, out var byType))
                _quantities[key] = byType = new Dictionary<Guid, int>();
            byType[addTypeId] = Math.Max(0, value);
        }

        private int TotalAdds => CurrentKeys.Sum(k => CountFor(k) * _addTypes.Sum(a => GetQty(k, a.AddTypeId)));

        private bool CanContinue => _step switch
        {
            0 => _rows.Count > 0 && !_reading,
            1 => _mode is not null,
            2 => _addTypes.Count > 0 && TotalAdds > 0,
            _ => false,
        };

        private string ModeName => _mode == AddAssignmentMode.ByEmployeeType ? "tipo de empleado" : "área";

        // ---------- pantalla ----------

        private void Refresh()
        {
            Step0.Visibility = _finished ? Visibility.Collapsed : _step == 0 ? Visibility.Visible : Visibility.Collapsed;
            Step1.Visibility = !_finished && _step == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2.Visibility = !_finished && _step == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3.Visibility = !_finished && _step == 3 ? Visibility.Visible : Visibility.Collapsed;
            StepDone.Visibility = _finished ? Visibility.Visible : Visibility.Collapsed;
            NavBar.Visibility = _finished ? Visibility.Collapsed : Visibility.Visible;
            Steps.Visibility = _finished ? Visibility.Collapsed : Visibility.Visible;

            BtnPrev.IsEnabled = _step > 0 && !_saving;
            BtnPrev.Visibility = _step > 0 ? Visibility.Visible : Visibility.Hidden;
            BtnNext.Content = _step == 3 ? (_saving ? "Registrando…" : "Terminar y registrar") : "Siguiente";
            BtnNext.IsEnabled = _step == 3 ? !_saving : CanContinue;

            DrawStepper();
        }

        private void DrawStepper()
        {
            Steps.Children.Clear();
            for (var i = 0; i < _titles.Length; i++)
            {
                var active = i == _step;
                var done = i < _step;
                var chip = new Border
                {
                    CornerRadius = new CornerRadius(14),
                    Padding = new Thickness(12, 6, 14, 6),
                    Margin = new Thickness(0, 0, 8, 0),
                    Background = active ? (Brush)FindResource("BrushCyan") : (Brush)FindResource("BrushNavy"),
                    Child = new TextBlock
                    {
                        Text = $"{(done ? "✓" : (i + 1).ToString())}  {_titles[i]}",
                        FontSize = 12,
                        FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal,
                        Foreground = active ? (Brush)FindResource("BrushNight") : (Brush)FindResource(done ? "BrushCyan" : "BrushLavender"),
                    },
                };
                Steps.Children.Add(chip);
            }
        }

        private void ShowReadResult(string message, bool ok)
        {
            TxtReadResult.Text = message;
            TxtReadResult.Foreground = (Brush)FindResource(ok ? "BrushSuccess" : "BrushError");
            TxtReadResult.Visibility = Visibility.Visible;
        }

        // ---------- paso 1 ----------

        private async void OnPickFile(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", Title = "Archivo de empleados" };
            if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

            _reading = true;
            BtnFile.IsEnabled = false;
            TxtReading.Visibility = Visibility.Visible;
            TxtReadResult.Visibility = Visibility.Collapsed;
            TxtWarnings.Visibility = Visibility.Collapsed;
            _rows = new(); _warnings = new(); _areas = new(); _types = new();
            _quantities.Clear();
            _mode = null;
            Refresh();

            try
            {
                var bytes = await File.ReadAllBytesAsync(dialog.FileName);
                var read = await Task.Run(() => EmployeeExcelReader.Read(new MemoryStream(bytes)));

                _rows = read.Rows;
                _warnings = read.Warnings;
                _areas = _rows.Select(r => r.AreaName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
                _types = _rows.Select(r => r.EmployeeTypeName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

                if (_rows.Count == 0)
                    ShowReadResult("No se encontró ningún empleado válido en el archivo.", ok: false);
                else
                    ShowReadResult($"{Path.GetFileName(dialog.FileName)}: se leyeron {_rows.Count} empleados, {_areas.Count} áreas y {_types.Count} tipos de empleado.", ok: true);

                if (_warnings.Count > 0)
                {
                    var lines = _warnings.Take(10).ToList();
                    if (_warnings.Count > 10) lines.Add($"… y {_warnings.Count - 10} más.");
                    TxtWarnings.Text = $"Se omitieron {_warnings.Count} renglones incompletos:\n" + string.Join("\n", lines);
                    TxtWarnings.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ShowReadResult($"No se pudo leer el archivo: {ex.GetBaseException().Message}", ok: false);
            }
            finally
            {
                _reading = false;
                BtnFile.IsEnabled = true;
                TxtReading.Visibility = Visibility.Collapsed;
                Refresh();
            }
        }

        // ---------- paso 2 ----------

        private void FillStep2()
        {
            LblAreas.Text = $"Áreas ({_areas.Count})";
            LblTypes.Text = $"Tipos de empleado ({_types.Count})";
            FillChips(ChipsAreas, _areas);
            FillChips(ChipsTypes, _types);
            RbArea.IsChecked = _mode == AddAssignmentMode.ByArea;
            RbType.IsChecked = _mode == AddAssignmentMode.ByEmployeeType;
        }

        private void FillChips(WrapPanel panel, IEnumerable<string> values)
        {
            panel.Children.Clear();
            foreach (var value in values)
            {
                panel.Children.Add(new Border
                {
                    CornerRadius = new CornerRadius(12),
                    BorderThickness = new Thickness(1),
                    BorderBrush = (Brush)FindResource("BrushLine"),
                    Padding = new Thickness(10, 3, 10, 3),
                    Margin = new Thickness(0, 0, 6, 6),
                    Child = new TextBlock { Text = value, FontSize = 12, Foreground = (Brush)FindResource("BrushLavender") },
                });
            }
        }

        private void OnModeChanged(object sender, RoutedEventArgs e)
        {
            if (RbArea is null || RbType is null) return;

            var mode = RbArea.IsChecked == true ? AddAssignmentMode.ByArea
                     : RbType.IsChecked == true ? AddAssignmentMode.ByEmployeeType
                     : (AddAssignmentMode?)null;
            if (mode == _mode) return;

            _mode = mode;
            _quantities.Clear(); // las cantidades dependen del rubro elegido
            Refresh();
        }

        // ---------- paso 3 ----------

        private void FillStep3()
        {
            var hasTypes = _addTypes.Count > 0;
            TxtNoAddTypes.Visibility = hasTypes ? Visibility.Collapsed : Visibility.Visible;
            LblQtyTitle.Text = $"Adicionales por {ModeName}";
            LblQtyTitle.Visibility = LblQtyHelp.Visibility = QtyGrid.Visibility = hasTypes ? Visibility.Visible : Visibility.Collapsed;
            if (hasTypes)
                BuildMatrix(QtyGrid, editable: true);
        }

        // Tabla rubro × adicional. En modo editable cada celda es un cuadro numérico.
        private void BuildMatrix(Grid grid, bool editable)
        {
            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 160 });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            foreach (var _ in _addTypes)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(editable ? 120 : 110) });

            void Cell(UIElement element, int row, int col)
            {
                Grid.SetRow(element, row);
                Grid.SetColumn(element, col);
                grid.Children.Add(element);
            }
            TextBlock Text(string text, bool header, bool right = false) => new()
            {
                Text = text,
                Foreground = (Brush)FindResource(header ? "BrushLavender" : "BrushWhite"),
                FontSize = header ? 12 : 14,
                Margin = new Thickness(10, 8, 10, 8),
                HorizontalAlignment = right ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Cell(Text(_mode == AddAssignmentMode.ByEmployeeType ? "TIPO DE EMPLEADO" : "ÁREA", true), 0, 0);
            Cell(Text("EMPLEADOS", true, right: true), 0, 1);
            for (var c = 0; c < _addTypes.Count; c++)
                Cell(Text(editable ? _addTypes[c].AddTypeName : $"{_addTypes[c].AddTypeName} (c/u)", true, right: true), 0, c + 2);

            var r = 1;
            foreach (var key in CurrentKeys)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                Cell(Text(key, false), r, 0);
                Cell(Text(CountFor(key).ToString(), false, right: true), r, 1);

                for (var c = 0; c < _addTypes.Count; c++)
                {
                    var addId = _addTypes[c].AddTypeId;
                    if (editable)
                    {
                        var box = new TextBox
                        {
                            Text = GetQty(key, addId).ToString(),
                            Padding = new Thickness(8, 5, 8, 5),
                            Margin = new Thickness(6, 3, 6, 3),
                            TextAlignment = TextAlignment.Right,
                            MaxLength = 2,
                        };
                        var capturedKey = key;
                        box.PreviewTextInput += (_, e) => e.Handled = !e.Text.All(char.IsDigit);
                        box.GotKeyboardFocus += (_, _) => box.SelectAll();
                        box.TextChanged += (_, _) =>
                        {
                            SetQty(capturedKey, addId, int.TryParse(box.Text, out var v) ? v : 0);
                            Refresh();
                        };
                        Cell(box, r, c + 2);
                    }
                    else
                    {
                        Cell(Text(GetQty(key, addId).ToString(), false, right: true), r, c + 2);
                    }
                }
                r++;
            }
        }

        // ---------- paso 4 ----------

        private void FillStep4()
        {
            SumEmployees.Text = _rows.Count.ToString();
            SumMode.Text = _mode == AddAssignmentMode.ByEmployeeType ? "Tipo de empleado" : "Área";
            SumAdds.Text = TotalAdds.ToString();
            SumCreate.Text = $"{_areas.Count} áreas y {_types.Count} tipos de empleado se crearán solo si aún no existen.";
            BuildMatrix(SummaryGrid, editable: false);
            TxtSaveError.Visibility = Visibility.Collapsed;
        }

        // ---------- navegación ----------

        private void OnPrev(object sender, RoutedEventArgs e)
        {
            if (_step == 0 || _saving) return;
            _step--;
            Enter();
        }

        private async void OnNext(object sender, RoutedEventArgs e)
        {
            if (_step < 3)
            {
                if (!CanContinue) return;
                _step++;
                Enter();
                return;
            }
            await RegisterAsync();
        }

        private void Enter()
        {
            switch (_step)
            {
                case 1: FillStep2(); break;
                case 2: FillStep3(); break;
                case 3: FillStep4(); break;
            }
            Refresh();
        }

        private async Task RegisterAsync()
        {
            _saving = true;
            TxtSaveError.Visibility = Visibility.Collapsed;
            Refresh();
            try
            {
                var request = new EmployeeImportRequest
                {
                    Employees = _rows,
                    Mode = _mode ?? AddAssignmentMode.ByArea,
                    Allocations = CurrentKeys
                        .SelectMany(k => _addTypes.Select(a => new AddAllocation
                        {
                            Key = k,
                            AddTypeId = a.AddTypeId,
                            Quantity = GetQty(k, a.AddTypeId),
                        }))
                        .Where(a => a.Quantity > 0)
                        .ToList(),
                };

                using var scope = _services.CreateScope();
                var result = await scope.ServiceProvider.GetRequiredService<IEmployeeService>().RegisterEmployeeImportAsync(request);

                if (result.IsSuccess)
                {
                    _finished = true;
                    TxtDone.Text = result.Message;
                }
                else
                {
                    TxtSaveError.Text = result.Message;
                    TxtSaveError.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                TxtSaveError.Text = $"No se pudo registrar la carga: {ex.GetBaseException().Message}";
                TxtSaveError.Visibility = Visibility.Visible;
            }
            finally
            {
                _saving = false;
                Refresh();
            }
        }

        private void OnStartOver(object sender, RoutedEventArgs e)
        {
            _rows = new(); _warnings = new(); _areas = new(); _types = new();
            _quantities.Clear();
            _mode = null;
            _step = 0;
            _finished = false;
            TxtReadResult.Visibility = Visibility.Collapsed;
            TxtWarnings.Visibility = Visibility.Collapsed;
            Refresh();
        }
    }
}
