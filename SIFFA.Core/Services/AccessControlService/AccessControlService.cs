using Microsoft.EntityFrameworkCore;
﻿using SIFFA.Core.Models;
using SIFFA.Core.Models.Custom;
using SIFFA.Core.Models.Custom.Model;
using SIFFA.Core.Models.Custom.Request.AccessControl;
using SIFFA.Core.Models.Custom.Response;

namespace SIFFA.Core.Services.AccessControlService
{
    public class AccessControlService : IAccessControlService
    {
        public AccessControlService(SiffaContext db)
        {
            Db = db;
        }

        private SiffaContext Db { get; set; }

        public AddTraceResponse AddTrace(InsertTraceAccessControl request)
        {
            string movementType;
            Employee? employee;
            AccessAction? accessAction;
            AccessType againstAccessType;

            // Nombre de la acción en el catálogo [Catalog].[AccessAction]
            movementType = request.AccessType == AccessType.Entry ? "Ingreso" : "Salida";


            //If the access type is exit, check if the employee has an entry trace registered
            if (request.AccessType == AccessType.Exit)
            {
                // Check if the employee has an entry trace registered
                var identifierMaster = request.IdentifierNumber?.Trim();
                var income = (from tr in Db.Traces
                              join at in Db.AccessActions on tr.AccessActionId equals at.AccessActionId
                              where tr.Employee.InernalIdentifier == identifierMaster && at.AccessActionName == "Ingreso"
                              select tr)
                              .ToList()
                              .FirstOrDefault();

                if (income is null)
                    return new AddTraceResponse(false, "El empleado no tiene un registro de ingreso", null);
            }


            var identifier = request.IdentifierNumber?.Trim();
            if (string.IsNullOrEmpty(identifier))
                return new AddTraceResponse(false, "Captura el código o número de empleado", null);

            // Empleado
            employee = Db.Employees.FirstOrDefault(e => e.InernalIdentifier == identifier);
            if (employee is null)
                return new AddTraceResponse(false, "Empleado no encontrado", null);

            var fullName = $"{employee.FirstName} {employee.LastName}".Trim();

            // Acción de acceso (Ingreso / Salida)
            accessAction = Db.AccessActions.FirstOrDefault(a => a.AccessActionName == movementType);
            if (accessAction is null)
                return new AddTraceResponse(false, $"Falta la acción {movementType} en el catálogo de acciones de acceso", null);

            // ¿Ya existe ese movimiento para el empleado?
            var alreadyRegistered = Db.Traces.Any(t => t.EmployeeId == employee.EmployeeId
                                                    && t.AccessActionId == accessAction.AccessActionId);
            if (alreadyRegistered)
                return new AddTraceResponse(false, $"{movementType} ya existente para el usuario {fullName}", null,
                    employee.InernalIdentifier, fullName);

            // Registro del movimiento
            var now = DateTime.Now;
            Db.Traces.Add(new Trace
            {
                Employee = employee,
                AccessAction = accessAction,
                TraceDate = now,
                InsertionDate = now,
                TraceDescription = $"{movementType} de {fullName}",
            });
            Db.SaveChanges();

            // Cada fila de [Add].[AddEmployee] es un boleto: se agrupan por tipo para mostrar la cantidad.
            var employeeAdds = Db.AddEmployees
                .Where(a => a.EmployeeId == employee.EmployeeId)
                .GroupBy(a => new { a.AddTypeId, a.AddType.AddTypeName })
                .Select(g => new
                {
                    g.Key.AddTypeId,
                    g.Key.AddTypeName,
                    Quantity = g.Count(),
                    Delivered = g.Count(x => x.IsDeliveryAdd),
                })
                .OrderBy(g => g.AddTypeName)
                .ToList()
                .Select(g => new AddEmployeeModel(g.AddTypeName, g.Quantity, g.Delivered == g.Quantity, g.AddTypeId))
                .ToList();

            return new AddTraceResponse(true, $"{movementType} de {fullName} agregado correctamente", employeeAdds,
                employee.InernalIdentifier, fullName);
        }

        public List<EmployeeSearchResult> SearchEmployees(string term)
        {
            term = (term ?? string.Empty).Trim();
            if (term.Length == 0)
                return new List<EmployeeSearchResult>();

            // Coincidencia exacta de número o correo: es lo que devuelve un lector de códigos.
            var exact = Db.Employees.AsNoTracking()
                .Where(e => e.InernalIdentifier == term || e.EmployeeEmail == term)
                .Select(e => new EmployeeSearchResult(e.InernalIdentifier, (e.FirstName + " " + e.LastName).Trim(), e.EmployeeEmail, e.Area.Description))
                .ToList();
            if (exact.Count > 0)
                return exact;

            return Db.Employees.AsNoTracking()
                .Where(e => (e.FirstName + " " + e.LastName).Contains(term)
                         || e.EmployeeEmail.Contains(term)
                         || e.InernalIdentifier.Contains(term))
                .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
                .Take(10)
                .Select(e => new EmployeeSearchResult(e.InernalIdentifier, (e.FirstName + " " + e.LastName).Trim(), e.EmployeeEmail, e.Area.Description))
                .ToList();
        }

        public GenericReponse SaveDeliveries(SaveDeliveriesRequest request)
        {
            var employee = Db.Employees.FirstOrDefault(e => e.InernalIdentifier == request.EmployeeNumber);
            if (employee is null)
                return new GenericReponse(false, "Empleado no encontrado");

            if (request.DeliveredAddTypeIds.Count == 0)
                return new GenericReponse(false, "No se marcó ningún boleto como entregado");

            var pending = Db.AddEmployees
                .Where(a => a.EmployeeId == employee.EmployeeId
                         && request.DeliveredAddTypeIds.Contains(a.AddTypeId)
                         && !a.IsDeliveryAdd)
                .ToList();

            var now = DateTime.Now;
            foreach (var add in pending)
            {
                add.IsDeliveryAdd = true;
                add.DeliveryDate = now;
            }
            Db.SaveChanges();

            return new GenericReponse(true, $"Se guardaron {pending.Count} boletos entregados");
        }

        bool IAccessControlService.ValidateTrace(InsertTraceAccessControl request)
        {
            var identifier = request.IdentifierNumber?.Trim();
            return Db.Traces.Any(t => t.Employee.InernalIdentifier == identifier);
        }
    }
}
