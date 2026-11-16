using Microsoft.EntityFrameworkCore;
using SIFFA.Core.Models;
using SIFFA.Core.Models.Custom;
using SIFFA.Core.Models.Custom.Request.Employeees;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIFFA.Core.Services.EmployeeService
{
    public class EmployeeService : IEmployeeService
    {
        public EmployeeService(SiffaContext db)
        {
            Db = db;
        }

        private SiffaContext Db { get; set; }
        public GenericReponse AddSeveralEmployees(List<InsertEmployeeRequest> employees)
        {
            List<Employee>? employeesNew;
            Area? area;
            List<Area> areas;



            employeesNew = new List<Employee>();
            areas =  new List<Area>();
            try
            {
                ////Distinct all areas
                //foreach (var item in employees)
                //{
                //    if (item.AreaName != null &&
                //        areas.FirstOrDefault(a => a.Description == item.AreaName) == null)
                //    {
                //        area = new Area();
                //        area.Description = item.AreaName;
                //        areas.Add(area);
                //    }
                //}

                //Distinct All employees
                foreach (var item in employees)
                {
                    if (item.InternalIdentificer != null &&
                        employeesNew.FirstOrDefault(e => e.InernalIdentifier == item.InternalIdentificer) == null)
                    {
                        Employee employee = new Employee
                        {
                            InernalIdentifier = item.InternalIdentificer,
                            FirstName = item.FirstName,
                            LastName = item.LastName,
                            EmployeeEmail = item.EmployeeEmail,
                            Area = area = new Area() { Description = item.AreaName }
                        };
                        employeesNew.Add(employee);
                    }
                }

                //Insert all employees
                Db.Employees.AddRange(employeesNew);
                Db.SaveChanges();   

            }
            catch (Exception ex)
            {
                return new GenericReponse(false, $"Error al agregar los empleados: {ex.Message}");
            }
            return new GenericReponse(true, "Empleados agregados correctamente");
        }

        // ───────── Tarjetas de invitación ─────────

        private async Task<List<InvitationCardData>> LoadCardsAsync(string? employeeNumber = null)
        {
            var query = Db.Employees.AsNoTracking().AsQueryable();
            if (employeeNumber is not null)
                query = query.Where(e => e.InernalIdentifier == employeeNumber);

            var employees = await query
                .OrderBy(e => e.InernalIdentifier)
                .Select(e => new
                {
                    e.EmployeeId,
                    e.InernalIdentifier,
                    e.FirstName,
                    e.LastName,
                    Area = e.Area.Description,
                    Type = e.EmployeeType.EmployeeTypeName,
                })
                .ToListAsync();

            var employeeIds = employees.Select(e => e.EmployeeId).ToList();
            var adds = (await Db.AddEmployees.AsNoTracking()
                    .Where(a => employeeNumber == null || employeeIds.Contains(a.EmployeeId))
                    .GroupBy(a => new { a.EmployeeId, a.AddType.AddTypeName })
                    .Select(g => new { g.Key.EmployeeId, g.Key.AddTypeName, Quantity = g.Count() })
                    .ToListAsync())
                .ToLookup(a => a.EmployeeId);

            return employees.Select(e => new InvitationCardData
            {
                EmployeeNumber = e.InernalIdentifier,
                FullName = $"{e.FirstName} {e.LastName}".Trim(),
                Area = e.Area,
                EmployeeType = e.Type,
                Adds = adds[e.EmployeeId]
                    .OrderBy(a => a.AddTypeName)
                    .Select(a => new InvitationCardAdd(a.AddTypeName, a.Quantity))
                    .ToList(),
            }).ToList();
        }

        public async Task<InvitationCardData?> GetCardAsync(string employeeNumber)
        {
            var cards = await LoadCardsAsync(employeeNumber);
            return cards.FirstOrDefault();
        }

        public async Task<InvitationCardData?> GetCardSampleAsync()
        {
            var cards = await LoadCardsAsync();
            // Se prefiere un empleado con adicionales para que la muestra sea representativa.
            return cards.FirstOrDefault(c => c.Adds.Count > 0) ?? cards.FirstOrDefault();
        }

        public byte[]? RenderCardPreview(InvitationCardData card, byte[] logo, CardCodeType code = CardCodeType.Qr)
        {
            using var bitmap = InvitationCardRenderer.DecodeLogo(logo);
            return bitmap is null ? null : InvitationCardRenderer.RenderPng(card, bitmap, code);
        }

        public async Task<CardsZipResult> GenerateCardsZipAsync(byte[] logo, CardFormat format, CardCodeType code = CardCodeType.Qr)
        {
            using var bitmap = InvitationCardRenderer.DecodeLogo(logo);
            if (bitmap is null)
                return new CardsZipResult(false, "El logo no es una imagen válida.");

            var cards = await LoadCardsAsync();
            if (cards.Count == 0)
                return new CardsZipResult(false, "No hay empleados registrados para generar tarjetas.");

            var extension = format == CardFormat.Pdf ? "pdf" : "png";

            // El dibujo es trabajo de CPU: se hace fuera del hilo de la conexión.
            return await Task.Run(() =>
            {
                try
                {
                    // Dibujo en paralelo (cada tarjeta usa su propio lienzo); el logo es de solo lectura.
                    var files = new byte[cards.Count][];
                    Parallel.For(0, cards.Count, i =>
                    {
                        files[i] = format == CardFormat.Pdf
                            ? InvitationCardRenderer.RenderPdf(cards[i], bitmap, code)
                            : InvitationCardRenderer.RenderPng(cards[i], bitmap, code);
                    });

                    using var zipStream = new MemoryStream();
                    using (var zip = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, true))
                    {
                        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        for (var i = 0; i < cards.Count; i++)
                        {
                            var card = cards[i];
                            var file = files[i];

                            var name = SafeFileName($"{card.EmployeeNumber}_{card.FullName}");
                            var unique = name;
                            for (var n = 2; !usedNames.Add(unique); n++)
                                unique = $"{name}_{n}";

                            // PNG y PDF ya vienen comprimidos: no se recomprime para ir más rápido.
                            var entry = zip.CreateEntry($"{unique}.{extension}", System.IO.Compression.CompressionLevel.NoCompression);
                            using var entryStream = entry.Open();
                            entryStream.Write(file, 0, file.Length);
                        }
                    }

                    return new CardsZipResult(true, $"Se generaron {cards.Count} tarjetas.", zipStream.ToArray(),
                        $"tarjetas_invitacion_{extension}.zip", cards.Count);
                }
                catch (Exception ex)
                {
                    return new CardsZipResult(false, $"Error al generar las tarjetas: {ex.Message}");
                }
            });
        }

        private static string SafeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
            return clean.Length == 0 ? "tarjeta" : clean;
        }

        public async Task<List<AddType>> GetAddTypesAsync() =>
            await Db.AddTypes.AsNoTracking().OrderBy(a => a.AddTypeName).ToListAsync();

        public async Task<GenericReponse> RegisterEmployeeImportAsync(EmployeeImportRequest request)
        {
            if (request.Employees.Count == 0)
                return new GenericReponse(false, "No hay empleados para registrar.");

            if (request.Allocations.Any(a => a.Quantity < 0))
                return new GenericReponse(false, "La cantidad de adicionales no puede ser negativa.");

            try
            {
                var comparer = StringComparer.OrdinalIgnoreCase;
                Func<EmployeeImportRow, string> keyOf = request.Mode == AddAssignmentMode.ByArea
                    ? r => r.AreaName
                    : r => r.EmployeeTypeName;

                // Los adicionales deben existir en el catálogo.
                var addTypeIds = request.Allocations.Where(a => a.Quantity > 0).Select(a => a.AddTypeId).Distinct().ToList();
                var knownAddTypes = await Db.AddTypes.Where(a => addTypeIds.Contains(a.AddTypeId)).Select(a => a.AddTypeId).ToListAsync();
                if (knownAddTypes.Count != addTypeIds.Count)
                    return new GenericReponse(false, "Alguno de los tipos de adicional ya no existe en el catálogo.");

                // 1) Áreas y tipos de empleado: se reutilizan los existentes y solo se crean los que faltan.
                var areas = (await Db.Areas.ToListAsync()).GroupBy(a => a.Description, comparer).ToDictionary(g => g.Key, g => g.First(), comparer);
                var types = (await Db.EmployeeTypes.ToListAsync()).GroupBy(t => t.EmployeeTypeName, comparer).ToDictionary(g => g.Key, g => g.First(), comparer);
                var newAreas = 0;
                var newTypes = 0;

                foreach (var name in request.Employees.Select(e => e.AreaName.Trim()).Distinct(comparer))
                {
                    if (areas.ContainsKey(name)) continue;
                    var area = new Area { Description = name };
                    Db.Areas.Add(area);
                    areas[name] = area;
                    newAreas++;
                }

                foreach (var name in request.Employees.Select(e => e.EmployeeTypeName.Trim()).Distinct(comparer))
                {
                    if (types.ContainsKey(name)) continue;
                    var type = new EmployeeType { EmployeeTypeName = name };
                    Db.EmployeeTypes.Add(type);
                    types[name] = type;
                    newTypes++;
                }

                // Configuración de adicionales por rubro, en [Add].[AddTypeEmployee]:
                // se actualiza la cantidad si esa combinación ya existe y se inserta si no.
                var existingConfigs = await Db.AddTypeEmployees.ToListAsync();
                var configsSaved = 0;
                foreach (var allocation in request.Allocations.Where(a => a.Quantity > 0))
                {
                    var key = allocation.Key.Trim();
                    Area? area = null;
                    EmployeeType? type = null;
                    if (request.Mode == AddAssignmentMode.ByArea ? !areas.TryGetValue(key, out area) : !types.TryGetValue(key, out type))
                        continue;

                    var config = existingConfigs.FirstOrDefault(c => c.AddTypeId == allocation.AddTypeId
                        && (request.Mode == AddAssignmentMode.ByArea
                            ? c.AreaId != null && c.AreaId == area!.AreaId
                            : c.EmployeeTypeId != null && c.EmployeeTypeId == type!.EmployeeTypeId));

                    if (config is null)
                    {
                        Db.AddTypeEmployees.Add(new AddTypeEmployee
                        {
                            AddTypeId = allocation.AddTypeId,
                            Area = area,
                            EmployeeType = type,
                            Quantity = allocation.Quantity,
                        });
                    }
                    else
                    {
                        config.Quantity = allocation.Quantity;
                    }
                    configsSaved++;
                }

                // 2) Empleados: sin repetir dentro del archivo ni contra los que ya existen (número y correo son únicos).
                var existingNumbers = (await Db.Employees.Select(e => e.InernalIdentifier).ToListAsync()).ToHashSet(comparer);
                var existingEmails = (await Db.Employees.Select(e => e.EmployeeEmail).ToListAsync()).ToHashSet(comparer);
                var seenNumbers = new HashSet<string>(comparer);
                var seenEmails = new HashSet<string>(comparer);
                var skipped = 0;
                var inserted = 0;
                var addsInserted = 0;

                var allocationsByKey = request.Allocations
                    .Where(a => a.Quantity > 0)
                    .GroupBy(a => a.Key.Trim(), comparer)
                    .ToDictionary(g => g.Key, g => g.ToList(), comparer);

                foreach (var row in request.Employees)
                {
                    var number = row.InternalIdentifier.Trim();
                    var email = row.EmployeeEmail.Trim();

                    if (existingNumbers.Contains(number) || existingEmails.Contains(email)
                        || !seenNumbers.Add(number) || !seenEmails.Add(email))
                    {
                        skipped++;
                        continue;
                    }

                    var employee = new Employee
                    {
                        EmployeeId = Guid.NewGuid(),
                        InernalIdentifier = number,
                        FirstName = row.FirstName.Trim(),
                        LastName = row.LastName?.Trim(),
                        EmployeeEmail = email,
                        Area = areas[row.AreaName.Trim()],
                        EmployeeType = types[row.EmployeeTypeName.Trim()],
                    };
                    Db.Employees.Add(employee);
                    inserted++;

                    // 3) Adicionales del empleado: una fila por cada boleto.
                    if (allocationsByKey.TryGetValue(keyOf(row).Trim(), out var allocations))
                    {
                        foreach (var allocation in allocations)
                        {
                            for (var i = 0; i < allocation.Quantity; i++)
                            {
                                Db.AddEmployees.Add(new AddEmployee
                                {
                                    AddTypeId = allocation.AddTypeId,
                                    Employee = employee,
                                });
                                addsInserted++;
                            }
                        }
                    }
                }

                // Una sola llamada = una sola transacción; EF inserta en orden de dependencias
                // (áreas y tipos, luego empleados, luego adicionales).
                await Db.SaveChangesAsync();

                var message = $"Se registraron {inserted} empleados, {addsInserted} adicionales, {configsSaved} cantidades configuradas, {newAreas} áreas nuevas y {newTypes} tipos de empleado nuevos.";
                if (skipped > 0)
                    message += $" Se omitieron {skipped} empleados repetidos (número o correo ya existente).";
                return new GenericReponse(true, message);
            }
            catch (Exception ex)
            {
                return new GenericReponse(false, $"Error al registrar la carga: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}
