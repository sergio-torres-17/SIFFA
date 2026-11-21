using System.IO.Compression;
using SIFFA.Core.Models.Custom.Request.Employeees;

namespace SIFFA.Core.Services.EmployeeService
{
    /// <summary>
    /// Dibuja las tarjetas y las escribe en un .zip por lotes: la memoria se mantiene plana (no crece con el número
    /// de empleados) y la CPU no se satura. Lo usan el servicio con base de datos y el cliente de consola web.
    /// </summary>
    public static class CardsZipWriter
    {
        private const int BatchSize = 20;

        public static async Task<CardsZipResult> WriteAsync(IReadOnlyList<InvitationCardData> cards, byte[] logo, CardFormat format,
            CardCodeType code, Stream output, IProgress<CardsProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            using var bitmap = InvitationCardRenderer.DecodeLogo(logo);
            if (bitmap is null)
                return new CardsZipResult(false, "El logo no es una imagen válida.");

            if (cards.Count == 0)
                return new CardsZipResult(false, "No hay empleados registrados para generar tarjetas.");

            var extension = format == CardFormat.Pdf ? "pdf" : "png";
            // Solo la mitad de los núcleos: el equipo (o el servidor) sigue respondiendo mientras se generan.
            var parallelism = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2),
                CancellationToken = cancellationToken,
            };

            try
            {
                using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
                {
                    var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    progress?.Report(new CardsProgress(0, cards.Count));

                    for (var start = 0; start < cards.Count; start += BatchSize)
                    {
                        var count = Math.Min(BatchSize, cards.Count - start);
                        var batchStart = start;
                        var files = new byte[count][];

                        // El dibujo es trabajo de CPU: se hace fuera del hilo de la conexión. El logo es de solo lectura.
                        await Task.Run(() => Parallel.For(0, count, parallelism, i =>
                        {
                            var card = cards[batchStart + i];
                            files[i] = format == CardFormat.Pdf
                                ? InvitationCardRenderer.RenderPdf(card, bitmap, code)
                                : InvitationCardRenderer.RenderPng(card, bitmap, code);
                        }), cancellationToken);

                        for (var i = 0; i < count; i++)
                        {
                            var card = cards[start + i];
                            var name = SafeFileName($"{card.EmployeeNumber}_{card.FullName}");
                            var unique = name;
                            for (var n = 2; !usedNames.Add(unique); n++)
                                unique = $"{name}_{n}";

                            // PNG y PDF ya vienen comprimidos: no se recomprime para ir más rápido.
                            var entry = zip.CreateEntry($"{unique}.{extension}", CompressionLevel.NoCompression);
                            using var entryStream = entry.Open();
                            entryStream.Write(files[i], 0, files[i].Length);
                            files[i] = null!; // se libera apenas se escribe
                        }

                        progress?.Report(new CardsProgress(start + count, cards.Count));
                        await Task.Delay(10, cancellationToken); // respiro entre lotes
                    }
                }

                await output.FlushAsync(cancellationToken);
                return new CardsZipResult(true, $"Se generaron {cards.Count} tarjetas.", null,
                    $"tarjetas_invitacion_{extension}.zip", cards.Count);
            }
            catch (OperationCanceledException)
            {
                return new CardsZipResult(false, "La generación se canceló.");
            }
            catch (Exception ex)
            {
                return new CardsZipResult(false, $"Error al generar las tarjetas: {ex.Message}");
            }
        }

        private static string SafeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var clean = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
            return clean.Length == 0 ? "tarjeta" : clean;
        }
    }
}
