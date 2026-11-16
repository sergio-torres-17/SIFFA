using Microsoft.EntityFrameworkCore;
using SIFFA.Core.Models;
using SIFFA.Core.Models.Custom;

namespace SIFFA.Core.Services.AddTypeService
{
    public class AddTypeService : IAddTypeService
    {
        private const int MaxName = 50;
        private const int MaxDescription = 200;

        public AddTypeService(SiffaContext db)
        {
            Db = db;
        }

        private SiffaContext Db { get; set; }

        public async Task<List<AddTypeItem>> GetAllAsync() =>
            await Db.AddTypes.AsNoTracking()
                .OrderBy(a => a.AddTypeName)
                .Select(a => new AddTypeItem(
                    a.AddTypeId,
                    a.AddTypeName,
                    a.AddTypeDescription,
                    a.AddEmployees.Count,
                    a.AddTypeEmployees.Count))
                .ToListAsync();

        public async Task<GenericReponse> CreateAsync(string name, string? description)
        {
            var error = Validate(name, description, out name, out description);
            if (error is not null)
                return new GenericReponse(false, error);

            if (await Db.AddTypes.AnyAsync(a => a.AddTypeName == name))
                return new GenericReponse(false, "Ya existe un adicional con ese nombre.");

            Db.AddTypes.Add(new AddType { AddTypeName = name, AddTypeDescription = description });
            await Db.SaveChangesAsync();
            return new GenericReponse(true, "Adicional creado.");
        }

        public async Task<GenericReponse> UpdateAsync(Guid addTypeId, string name, string? description)
        {
            var error = Validate(name, description, out name, out description);
            if (error is not null)
                return new GenericReponse(false, error);

            var addType = await Db.AddTypes.FirstOrDefaultAsync(a => a.AddTypeId == addTypeId);
            if (addType is null)
                return new GenericReponse(false, "El adicional ya no existe.");

            if (await Db.AddTypes.AnyAsync(a => a.AddTypeName == name && a.AddTypeId != addTypeId))
                return new GenericReponse(false, "Ya existe otro adicional con ese nombre.");

            addType.AddTypeName = name;
            addType.AddTypeDescription = description;
            await Db.SaveChangesAsync();
            return new GenericReponse(true, "Adicional actualizado.");
        }

        public async Task<GenericReponse> DeleteAsync(Guid addTypeId)
        {
            var addType = await Db.AddTypes.FirstOrDefaultAsync(a => a.AddTypeId == addTypeId);
            if (addType is null)
                return new GenericReponse(false, "El adicional ya no existe.");

            var tickets = await Db.AddEmployees.CountAsync(a => a.AddTypeId == addTypeId);
            var configs = await Db.AddTypeEmployees.CountAsync(a => a.AddTypeId == addTypeId);
            if (tickets > 0 || configs > 0)
                return new GenericReponse(false,
                    $"No se puede eliminar: está asignado en {tickets} boletos de empleados y {configs} configuraciones por rubro.");

            Db.AddTypes.Remove(addType);
            await Db.SaveChangesAsync();
            return new GenericReponse(true, "Adicional eliminado.");
        }

        private static string? Validate(string? name, string? description, out string cleanName, out string? cleanDescription)
        {
            cleanName = (name ?? string.Empty).Trim();
            cleanDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

            if (cleanName.Length == 0)
                return "El nombre es obligatorio.";
            if (cleanName.Length > MaxName)
                return $"El nombre admite máximo {MaxName} caracteres.";
            if (cleanDescription is not null && cleanDescription.Length > MaxDescription)
                return $"La descripción admite máximo {MaxDescription} caracteres.";
            return null;
        }
    }
}
