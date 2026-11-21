using System.IO;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace SIFFA.DesktopApp.Services
{
    /// <summary>
    /// Monta la base de datos de SIFFA en el SQL Server configurado: crea la base si no existe, aplica la estructura
    /// (los scripts de SIFFA.Database van incrustados en la app) y carga los datos básicos. Se puede repetir sin riesgo:
    /// si la base ya tiene la estructura, no toca nada.
    /// </summary>
    public static class DatabaseProvisioner
    {
        // Mismo orden que usa el contenedor de Docker (docker/db/init.sh).
        private static readonly string[] TableFolders = ["Catalog", "Employee", "AccessControl", "Add", "Security"];

        public static async Task<(bool Ok, string Message)> ProvisionAsync(MachineSettings settings, IProgress<string>? progress = null)
        {
            try
            {
                var target = new SqlConnectionStringBuilder(settings.BuildConnectionString());
                var name = target.InitialCatalog;
                if (string.IsNullOrWhiteSpace(name))
                    return (false, "Escribe el nombre de la base de datos.");

                // 1) Crear la base si no existe (se conecta a master).
                target.InitialCatalog = "master";
                target.ConnectTimeout = 15;
                var created = false;
                await using (var master = new SqlConnection(target.ConnectionString))
                {
                    progress?.Report("Conectando al servidor…");
                    await master.OpenAsync();

                    await using var exists = master.CreateCommand();
                    exists.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = @n";
                    exists.Parameters.AddWithValue("@n", name);
                    if ((int)(await exists.ExecuteScalarAsync())! == 0)
                    {
                        progress?.Report($"Creando la base de datos {name}…");
                        await using var create = master.CreateCommand();
                        create.CommandText = $"CREATE DATABASE [{name.Replace("]", "]]")}]";
                        await create.ExecuteNonQueryAsync();
                        created = true;
                    }
                }

                // 2) Estructura y datos básicos, todo en una transacción: si algo falla, no queda a medias.
                target.InitialCatalog = name;
                await using var db = new SqlConnection(target.ConnectionString);
                await db.OpenAsync();

                await using (var check = db.CreateCommand())
                {
                    check.CommandText = "SELECT OBJECT_ID('Security.User')";
                    if (await check.ExecuteScalarAsync() is not (null or DBNull))
                        return (true, $"La base {name} ya está montada: no se cambió nada.");
                }

                await using var tx = (SqlTransaction)await db.BeginTransactionAsync();
                try
                {
                    var scripts = Scripts();
                    var n = 0;
                    foreach (var (file, sql) in scripts)
                    {
                        progress?.Report($"Creando estructura… {++n} de {scripts.Count}");
                        await using var cmd = db.CreateCommand();
                        cmd.Transaction = tx;
                        cmd.CommandText = sql;
                        cmd.CommandTimeout = 60;
                        try { await cmd.ExecuteNonQueryAsync(); }
                        catch (SqlException ex) { throw new InvalidOperationException($"{file}: {ex.Message}", ex); }
                    }

                    progress?.Report("Cargando datos básicos…");
                    await using var seed = db.CreateCommand();
                    seed.Transaction = tx;
                    seed.CommandText = "EXEC [Security].[Usp_Insert_Basics]; DROP PROCEDURE [Security].[Usp_Insert_Basics];";
                    await seed.ExecuteNonQueryAsync();

                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }

                return (true, (created ? $"Se creó la base {name} y su estructura." : $"Se montó la estructura en la base {name}.")
                    + " Al iniciar sesión se pedirá activar el sistema.");
            }
            catch (Exception ex)
            {
                return (false, $"No se pudo montar la base de datos: {ex.GetBaseException().Message}");
            }
        }

        // Scripts incrustados (SIFFA.Database\**\*.sql, nombre lógico "Db/<carpeta>/<archivo>.sql") en orden de ejecución.
        private static List<(string File, string Sql)> Scripts()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var all = assembly.GetManifestResourceNames()
                .Where(n => n.StartsWith("Db/", StringComparison.Ordinal) && n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                .Select(n => n.Replace('\\', '/'))
                .ToList();

            IEnumerable<string> In(string area) => all.Where(n => n.StartsWith($"Db/{area}/", StringComparison.Ordinal)).OrderBy(n => n, StringComparer.Ordinal);

            var ordered = new List<string>();
            ordered.AddRange(In("Schemas"));
            foreach (var folder in TableFolders)
                ordered.AddRange(all.Where(n => n.StartsWith($"Db/{folder}/Tables/", StringComparison.Ordinal)).OrderBy(n => n, StringComparer.Ordinal));
            ordered.AddRange(all.Where(n => n.Contains("/Indexes/")).OrderBy(n => n, StringComparer.Ordinal));
            ordered.AddRange(all.Where(n => n.Contains("/Foreign Keys/")).OrderBy(n => n, StringComparer.Ordinal));
            ordered.AddRange(all.Where(n => n.Contains("/Store Procedures/")).OrderBy(n => n, StringComparer.Ordinal));

            if (ordered.Count == 0)
                throw new InvalidOperationException("La app no trae los scripts de la base de datos.");

            var result = new List<(string, string)>();
            foreach (var key in ordered.Distinct())
            {
                // El nombre del recurso conserva el original (con \ o /); se busca el que coincide al normalizar.
                var resource = assembly.GetManifestResourceNames().First(n => n.Replace('\\', '/') == key);
                using var stream = assembly.GetManifestResourceStream(resource)!;
                using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
                result.Add((key["Db/".Length..], reader.ReadToEnd().TrimStart('﻿')));
            }
            return result;
        }
    }
}
