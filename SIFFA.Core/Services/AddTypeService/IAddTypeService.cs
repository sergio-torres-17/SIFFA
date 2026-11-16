using SIFFA.Core.Models.Custom;

namespace SIFFA.Core.Services.AddTypeService
{
    /// <summary>Un tipo de adicional (boleto de agua, comida, bebida...) con su uso actual.</summary>
    public record AddTypeItem(Guid AddTypeId, string Name, string? Description, int TicketsAssigned, int Configurations);

    /// <summary>Alta, edición y baja de los tipos de adicional del catálogo [Catalog].[AddType].</summary>
    public interface IAddTypeService
    {
        Task<List<AddTypeItem>> GetAllAsync();
        Task<GenericReponse> CreateAsync(string name, string? description);
        Task<GenericReponse> UpdateAsync(Guid addTypeId, string name, string? description);

        /// <summary>Elimina el tipo solo si nadie lo usa (ni boletos de empleados ni configuraciones por rubro).</summary>
        Task<GenericReponse> DeleteAsync(Guid addTypeId);
    }
}
