using ClosedXML.Excel;
using SIFFA.Core.Models.Custom.Request.Employeees;

namespace SIFFA.Core.Services.EmployeeService
{
    /// <summary>
    /// Lee el Excel de empleados. Columnas, en este orden (el renglón 1 son los encabezados):
    /// Tipo de empleado, Número, Nombre, Apellidos, Área, Correo.
    /// </summary>
    public static class EmployeeExcelReader
    {
        public static EmployeeExcelReadResult Read(Stream stream)
        {
            var result = new EmployeeExcelReadResult();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.First();
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

            for (var r = 2; r <= lastRow; r++)
            {
                string Cell(int c) => sheet.Cell(r, c).GetFormattedString().Trim();

                var row = new EmployeeImportRow
                {
                    EmployeeTypeName = Cell(1),
                    InternalIdentifier = Cell(2),
                    FirstName = Cell(3),
                    LastName = string.IsNullOrWhiteSpace(Cell(4)) ? null : Cell(4),
                    AreaName = Cell(5),
                    EmployeeEmail = Cell(6),
                };

                // Renglón completamente vacío: se ignora sin avisar.
                if (sheet.Row(r).IsEmpty())
                    continue;

                var missing = new List<string>();
                if (row.EmployeeTypeName.Length == 0) missing.Add("tipo de empleado");
                if (row.InternalIdentifier.Length == 0) missing.Add("número");
                if (row.FirstName.Length == 0) missing.Add("nombre");
                if (row.AreaName.Length == 0) missing.Add("área");
                if (row.EmployeeEmail.Length == 0) missing.Add("correo");

                if (missing.Count > 0)
                {
                    result.Warnings.Add($"Renglón {r} omitido: falta {string.Join(", ", missing)}.");
                    continue;
                }

                result.Rows.Add(row);
            }

            return result;
        }
    }
}
