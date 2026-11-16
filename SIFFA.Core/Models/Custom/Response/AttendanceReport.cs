namespace SIFFA.Core.Models.Custom.Response
{
    /// <summary>Situación de un empleado respecto al evento según sus movimientos de entrada y salida.</summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public enum AttendanceStatus
    {
        /// <summary>Su último movimiento fue una entrada: sigue dentro.</summary>
        Inside,
        /// <summary>No tiene ninguna entrada ni salida registrada.</summary>
        NotEntered,
        /// <summary>Su último movimiento fue una salida.</summary>
        Exited,
    }

    public class AttendanceRow
    {
        public string EmployeeNumber { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Area { get; set; } = "";
        public string EmployeeType { get; set; } = "";
        public AttendanceStatus Status { get; set; }
        /// <summary>Texto en español del estado, para mostrarlo en pantallas y reportes.</summary>
        public string StatusText { get; set; } = "";
        public DateTime? FirstEntry { get; set; }
        public DateTime? LastMovement { get; set; }
    }

    public class AttendanceReport
    {
        public DateTime GeneratedAt { get; set; }
        public int Total { get; set; }
        public int Inside { get; set; }
        public int NotEntered { get; set; }
        public int Exited { get; set; }
        /// <summary>Empleados del estado pedido (todos si no se filtró).</summary>
        public List<AttendanceRow> Employees { get; set; } = new();
    }
}
