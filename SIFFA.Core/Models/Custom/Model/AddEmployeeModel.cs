namespace SIFFA.Core.Models.Custom.Model
{
    /// <summary>
    /// Adicional (boleto) de un empleado agrupado por tipo: nombre, cantidad total y si ya se entregó todo.
    /// <paramref name="addTypeId"/> sirve para marcar la entrega al guardar.
    /// </summary>
    public record AddEmployeeModel(string name, int quantity, bool deliveryAdd, Guid addTypeId = default);

    /// <summary>Coincidencia al buscar un empleado por número, nombre o correo.</summary>
    public record EmployeeSearchResult(string EmployeeNumber, string FullName, string Email, string Area);

    /// <summary>Entregas confirmadas por el hoster para un empleado.</summary>
    public class SaveDeliveriesRequest
    {
        public string EmployeeNumber { get; set; } = string.Empty;
        /// <summary>Tipos de adicional marcados como entregados.</summary>
        public List<Guid> DeliveredAddTypeIds { get; set; } = new();
    }
}
