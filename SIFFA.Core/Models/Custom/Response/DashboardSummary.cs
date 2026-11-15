namespace SIFFA.Core.Models.Custom.Response
{
    public class DashboardSummary
    {
        public int TotalEmployees { get; set; }
        /// <summary>Empleados con al menos una entrada registrada.</summary>
        public int Entered { get; set; }
        /// <summary>Empleados que aún no han registrado entrada.</summary>
        public int Pending { get; set; }
        /// <summary>Empleados cuyo último movimiento fue una salida.</summary>
        public int Exited { get; set; }
        /// <summary>Empleados cuyo último movimiento fue una entrada (siguen dentro).</summary>
        public int Inside { get; set; }

        public List<DashboardAreaRow> ByArea { get; set; } = new();
        public List<DashboardHourRow> EntriesByHour { get; set; } = new();
        public List<DashboardAccessRow> LatestAccesses { get; set; } = new();

        /// <summary>true si el catálogo AccessAction aún no tiene acciones de entrada/salida reconocibles.</summary>
        public bool HasAccessActions { get; set; }
    }

    public class DashboardAreaRow
    {
        public string Area { get; set; } = string.Empty;
        public int Registered { get; set; }
        public int Entered { get; set; }
        public int Pending => Registered - Entered;
        public int Exited { get; set; }
    }

    public class DashboardHourRow
    {
        public int Hour { get; set; }
        public int Entries { get; set; }
    }

    public class DashboardAccessRow
    {
        public DateTime Date { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public bool IsEntry { get; set; }
    }
}
