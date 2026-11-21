using SIFFA.Core.Models;
using SIFFA.Core.Models.Custom;
using SIFFA.Core.Models.Custom.Request.Employeees;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIFFA.Core.Services.EmployeeService
{
    public interface IEmployeeService
    {
        public GenericReponse AddSeveralEmployees(List<InsertEmployeeRequest> employees);

        /// <summary>Tipos de adicional (boletos de bebida, comida, etc.) disponibles para asignar.</summary>
        public Task<List<AddType>> GetAddTypesAsync();

        /// <summary>
        /// Registra la carga masiva: primero áreas y tipos de empleado (sin repetir), después los empleados
        /// y al final los adicionales de cada empleado en [Add].[AddEmployee]. Todo en una sola transacción.
        /// </summary>
        public Task<GenericReponse> RegisterEmployeeImportAsync(EmployeeImportRequest request);

        /// <summary>Datos de la invitación de un empleado por su número; null si no existe.</summary>
        public Task<InvitationCardData?> GetCardAsync(string employeeNumber);

        /// <summary>Datos de la invitación de todos los empleados.</summary>
        public Task<List<InvitationCardData>> GetAllCardsAsync();

        /// <summary>Datos de un empleado registrado para usarlo de ejemplo en la previsualización; null si no hay empleados.</summary>
        public Task<InvitationCardData?> GetCardSampleAsync();

        /// <summary>Dibuja la tarjeta de un empleado como PNG, con el logo dado (null si el logo no es una imagen válida).</summary>
        public byte[]? RenderCardPreview(InvitationCardData card, byte[] logo, CardCodeType code = CardCodeType.Qr);

        /// <summary>
        /// Genera la tarjeta de invitación de todos los empleados en el formato elegido (PNG o PDF)
        /// y las escribe juntas en un .zip sobre <paramref name="output"/>, por lotes (memoria y CPU acotadas).
        /// </summary>
        public Task<CardsZipResult> GenerateCardsZipAsync(byte[] logo, CardFormat format, CardCodeType code,
            Stream output, IProgress<CardsProgress>? progress = null, CancellationToken cancellationToken = default);
    }
}
