using SIFFA.Core.Models.Custom;
using SIFFA.Core.Models.Custom.Model;
using SIFFA.Core.Models.Custom.Request.AccessControl;
using SIFFA.Core.Models.Custom.Response;

namespace SIFFA.Core.Services.AccessControlService
{
    public interface IAccessControlService
    {
        public AddTraceResponse AddTrace(InsertTraceAccessControl request);
        public bool ValidateTrace(InsertTraceAccessControl request);

        /// <summary>Busca empleados por número, nombre o correo (máximo 10 coincidencias).</summary>
        public List<EmployeeSearchResult> SearchEmployees(string term);

        /// <summary>Marca como entregados (IsDeliveryAdd) los adicionales indicados de un empleado.</summary>
        public GenericReponse SaveDeliveries(SaveDeliveriesRequest request);
    }
}
