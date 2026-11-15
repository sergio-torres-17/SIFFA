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

        /// <summary>Datos de un empleado registrado para usarlo de ejemplo en la previsualización; null si no hay empleados.</summary>
        public Task<InvitationCardData?> GetCardSampleAsync();

        /// <summary>Dibuja la tarjeta de un empleado como PNG, con el logo dado (null si el logo no es una imagen válida).</summary>
        public byte[]? RenderCardPreview(InvitationCardData card, byte[] logo, CardCodeType code = CardCodeType.Qr);

        /// <summary>
        /// Genera la tarjeta de invitación de todos los empleados en el formato elegido (PNG o PDF)
        /// y las devuelve juntas en un .zip.
        /// </summary>
        public Task<CardsZipResult> GenerateCardsZipAsync(byte[] logo, CardFormat format, CardCodeType code = CardCodeType.Qr);
    }
}
