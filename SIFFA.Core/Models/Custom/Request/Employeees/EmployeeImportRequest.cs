namespace SIFFA.Core.Models.Custom.Request.Employeees
{
    /// <summary>Un renglón del Excel de empleados.</summary>
    public class EmployeeImportRow
    {
        public string EmployeeTypeName { get; set; } = string.Empty;
        public string InternalIdentifier { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string AreaName { get; set; } = string.Empty;
        public string EmployeeEmail { get; set; } = string.Empty;
    }

    /// <summary>Rubro por el que se reparten los adicionales.</summary>
    public enum AddAssignmentMode
    {
        ByArea = 1,
        ByEmployeeType = 2,
    }

    /// <summary>
    /// Cantidad de adicionales de un tipo (AddType) para un rubro.
    /// <see cref="Key"/> es el nombre del área o del tipo de empleado, según el modo elegido.
    /// </summary>
    public class AddAllocation
    {
        public string Key { get; set; } = string.Empty;
        public Guid AddTypeId { get; set; }
        public int Quantity { get; set; }
    }

    public class EmployeeImportRequest
    {
        public List<EmployeeImportRow> Employees { get; set; } = new();
        public AddAssignmentMode Mode { get; set; } = AddAssignmentMode.ByArea;
        public List<AddAllocation> Allocations { get; set; } = new();
    }

    /// <summary>Resultado de leer el Excel: renglones válidos y avisos de los descartados.</summary>
    public class EmployeeExcelReadResult
    {
        public List<EmployeeImportRow> Rows { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
