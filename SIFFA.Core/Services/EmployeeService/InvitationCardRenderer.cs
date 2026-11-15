using QRCoder;
using SIFFA.Core.Models.Custom.Request.Employeees;
using SkiaSharp;

namespace SIFFA.Core.Services.EmployeeService
{
    /// <summary>
    /// Dibuja la tarjeta de invitación (1050 x 600) con SkiaSharp, la misma para PNG y PDF.
    /// El código (QR o de barras Code 128) contiene el número de empleado, para leerlo en el control de accesos.
    /// </summary>
    public static class InvitationCardRenderer
    {
        public const int Width = 1050;
        public const int Height = 600;

        // Se crean una sola vez y se comparten entre hilos (las tipografías de Skia son de solo lectura).
        private static readonly SKTypeface BoldFace = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold);
        private static readonly SKTypeface RegularFace = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal);

        private static readonly SKColor Night = SKColor.Parse("#0A1230");
        private static readonly SKColor Navy = SKColor.Parse("#16234F");
        private static readonly SKColor Cyan = SKColor.Parse("#22D3EE");
        private static readonly SKColor Lavender = SKColor.Parse("#B6C0E0");

        public static SKBitmap? DecodeLogo(byte[] logo)
        {
            try { return SKBitmap.Decode(logo); }
            catch { return null; }
        }

        public static byte[] RenderPng(InvitationCardData data, SKBitmap? logo, CardCodeType code = CardCodeType.Qr)
        {
            using var bitmap = new SKBitmap(Width, Height);
            using (var canvas = new SKCanvas(bitmap))
                Draw(canvas, data, logo, code);
            using var image = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }

        public static byte[] RenderPdf(InvitationCardData data, SKBitmap? logo, CardCodeType code = CardCodeType.Qr)
        {
            // La tarjeta se dibuja al doble de resolución y se incrusta como imagen: así el PDF pesa decenas de KB
            // en lugar de ~1 MB por incrustar la tipografía completa, y se ve igual que el PNG.
            using var bitmap = new SKBitmap(Width * 2, Height * 2);
            using (var raster = new SKCanvas(bitmap))
            {
                raster.Scale(2f);
                Draw(raster, data, logo, code);
            }
            using var image = SKImage.FromBitmap(bitmap);

            using var ms = new MemoryStream();
            using (var document = SKDocument.CreatePdf(ms))
            {
                // Página de 525 x 300 pt (7.3 x 4.2 in).
                using var canvas = document.BeginPage(Width / 2f, Height / 2f);
                canvas.DrawImage(image, new SKRect(0, 0, Width / 2f, Height / 2f),
                    new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
                document.EndPage();
                document.Close();
            }
            return ms.ToArray();
        }

        private static void Draw(SKCanvas canvas, InvitationCardData d, SKBitmap? logo, CardCodeType code)
        {
            var bold = BoldFace;
            var regular = RegularFace;

            // Fondo
            using (var bg = new SKPaint { Color = Night, IsAntialias = true })
                canvas.DrawRect(0, 0, Width, Height, bg);
            using (var panel = new SKPaint { Color = Navy, IsAntialias = true })
                canvas.DrawRoundRect(24, 24, Width - 48, Height - 48, 28, 28, panel);
            using (var accent = new SKPaint { Color = Cyan, IsAntialias = true })
                canvas.DrawRoundRect(24, 24, 14, Height - 48, 7, 7, accent);

            // Logo (arriba a la izquierda, ajustado a una caja de 260 x 110)
            if (logo is not null)
            {
                var box = new SKRect(70, 56, 330, 166);
                var scale = Math.Min(box.Width / logo.Width, box.Height / logo.Height);
                var w = logo.Width * scale;
                var h = logo.Height * scale;
                var dest = SKRect.Create(box.Left, box.Top + (box.Height - h) / 2, w, h);
                using var logoPaint = new SKPaint { IsAntialias = true };
                using var image = SKImage.FromBitmap(logo);
                canvas.DrawImage(image, dest, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), logoPaint);
            }

            // Encabezado
            DrawText(canvas, "INVITACIÓN", 70, 215, bold, 30, Cyan, maxWidth: 560);
            DrawText(canvas, $"Fiesta de fin de año {DateTime.Now.Year}", 70, 252, regular, 28, Lavender, maxWidth: 560);

            // Datos del empleado
            DrawText(canvas, d.FullName, 70, 335, bold, 54, SKColors.White, maxWidth: 600);
            DrawText(canvas, $"{d.Area}  ·  {d.EmployeeType}", 70, 385, regular, 30, Lavender, maxWidth: 600);
            DrawText(canvas, $"No. de empleado: {d.EmployeeNumber}", 70, 428, regular, 28, SKColors.White, maxWidth: 600);

            // Adicionales
            if (d.Adds.Count > 0)
            {
                var text = string.Join("   ", d.Adds.Select(a => $"{a.Name} ×{a.Quantity}"));
                DrawText(canvas, text, 70, 520, bold, 26, Cyan, maxWidth: 600);
            }

            if (code == CardCodeType.Barcode)
            {
                DrawBarcodeBlock(canvas, d.EmployeeNumber, bold, regular);
            }
            else
            {
                // QR sobre fondo blanco (derecha)
                const float qrBox = 260;
                var qrX = Width - 70 - qrBox;
                var qrY = 150f;
                using (var white = new SKPaint { Color = SKColors.White, IsAntialias = true })
                    canvas.DrawRoundRect(qrX - 14, qrY - 14, qrBox + 28, qrBox + 28, 18, 18, white);
                DrawQr(canvas, d.EmployeeNumber, qrX, qrY, qrBox);
                DrawText(canvas, "Presenta este código en el acceso", qrX + qrBox / 2, qrY + qrBox + 52, regular, 20, Lavender,
                    align: SKTextAlign.Center, maxWidth: qrBox + 60);
            }
        }

        // Código de barras Code 128 (lo genera ZXing; aquí solo se dibujan las barras).
        private static void DrawBarcodeBlock(SKCanvas canvas, string content, SKTypeface bold, SKTypeface regular)
        {
            const float boxW = 300;
            const float boxH = 200;
            var boxX = Width - 70 - boxW;
            var boxY = 190f;

            using (var white = new SKPaint { Color = SKColors.White, IsAntialias = true })
                canvas.DrawRoundRect(boxX, boxY, boxW, boxH, 18, 18, white);

            // Code 128 solo codifica ASCII imprimible; cualquier otro carácter se sustituye por "?".
            var text = new string(content.Select(c => c >= 32 && c <= 126 ? c : '?').ToArray());

            // Se pide el ancho mínimo (1 px por módulo) y se escala a mano para controlar el tamaño.
            var matrix = new ZXing.OneD.Code128Writer().encode(text, ZXing.BarcodeFormat.CODE_128, 1, 1);
            var modules = matrix.Width;
            const float inner = boxW - 20;
            var cell = Math.Max(1f, MathF.Floor(inner / modules));
            if (cell * modules > inner) cell = inner / modules; // texto muy largo: se encoge
            var barsW = cell * modules;
            var barsX = boxX + (boxW - barsW) / 2;
            var barsY = boxY + 24;
            const float barsH = 110;

            using var dark = new SKPaint { Color = Night, IsAntialias = false };
            for (var x = 0; x < modules; x++)
            {
                if (!matrix[x, 0]) continue;
                var start = x;
                while (x + 1 < modules && matrix[x + 1, 0]) x++;
                canvas.DrawRect(barsX + start * cell, barsY, (x - start + 1) * cell, barsH, dark);
            }

            DrawText(canvas, text, boxX + boxW / 2, boxY + 24 + barsH + 34, bold, 28, Night,
                align: SKTextAlign.Center, maxWidth: boxW - 24);
            DrawText(canvas, "Presenta este código en el acceso", boxX + boxW / 2, boxY + boxH + 36, regular, 20, Lavender,
                align: SKTextAlign.Center, maxWidth: boxW + 60);
        }

        private static void DrawQr(SKCanvas canvas, string content, float x, float y, float size)
        {
            using var generator = new QRCodeGenerator();
            using var qr = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var matrix = qr.ModuleMatrix;
            var count = matrix.Count;
            var cell = size / count;

            using var dark = new SKPaint { Color = Night, IsAntialias = false };
            for (var row = 0; row < count; row++)
                for (var col = 0; col < count; col++)
                    if (matrix[row][col])
                        canvas.DrawRect(x + col * cell, y + row * cell, cell + 0.5f, cell + 0.5f, dark);
        }

        private static void DrawText(SKCanvas canvas, string text, float x, float y, SKTypeface typeface, float size,
            SKColor color, SKTextAlign align = SKTextAlign.Left, float maxWidth = 0)
        {
            using var font = new SKFont(typeface, size);
            // Si el texto no cabe, se reduce el tamaño hasta que quepa.
            if (maxWidth > 0)
            {
                var width = font.MeasureText(text);
                if (width > maxWidth)
                    font.Size = Math.Max(12, size * maxWidth / width);
            }
            using var paint = new SKPaint { Color = color, IsAntialias = true };
            canvas.DrawText(text, x, y, align, font, paint);
        }
    }
}
