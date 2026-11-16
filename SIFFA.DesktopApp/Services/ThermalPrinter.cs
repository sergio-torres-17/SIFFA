using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SIFFA.DesktopApp.Services
{
    /// <summary>Imprime una imagen en cualquier impresora de Windows, pensado para impresoras térmicas de rollo (58 / 80 mm).</summary>
    public static class ThermalPrinter
    {
        private const double Margin = 4; // 1/96 de pulgada (≈ 1 mm)

        /// <summary>
        /// Muestra el cuadro de impresión de Windows y manda la imagen. Devuelve false si se cancela.
        /// </summary>
        /// <param name="vertical">Vertical gira el boleto 90° para que su lado largo avance con el papel y el corto quede a lo ancho del rollo.</param>
        /// <param name="highContrast">Blanco y negro puro: las térmicas solo imprimen negro o nada.</param>
        public static bool Print(BitmapSource image, bool vertical, bool highContrast, string description)
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true)
                return false;

            var source = Prepare(image, vertical, highContrast);

            // Se ajusta al área imprimible; en rollos continuos manda el ancho.
            var maxWidth = Math.Max(10, dialog.PrintableAreaWidth - Margin * 2);
            var maxHeight = Math.Max(10, dialog.PrintableAreaHeight - Margin * 2);
            var scale = Math.Min(maxWidth / source.Width, maxHeight / source.Height);
            var width = source.Width * scale;
            var height = source.Height * scale;

            var picture = new Image
            {
                Source = source,
                Width = width,
                Height = height,
                Stretch = Stretch.Fill,
                Margin = new Thickness(Margin),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            RenderOptions.SetBitmapScalingMode(picture, highContrast ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.Fant);

            var page = new Grid { Width = dialog.PrintableAreaWidth, Height = dialog.PrintableAreaHeight, Background = Brushes.White };
            page.Children.Add(picture);
            page.Measure(new Size(page.Width, page.Height));
            page.Arrange(new Rect(0, 0, page.Width, page.Height));
            page.UpdateLayout();

            dialog.PrintVisual(page, description);
            return true;
        }

        /// <summary>La imagen tal como se imprimirá (para la vista previa).</summary>
        public static BitmapSource Prepare(BitmapSource image, bool vertical, bool highContrast)
        {
            BitmapSource result = image;
            if (highContrast)
                result = ToBlackAndWhite(result);
            if (vertical)
            {
                var rotated = new TransformedBitmap(result, new RotateTransform(90));
                rotated.Freeze();
                result = rotated;
            }
            return result;
        }

        private static BitmapSource ToBlackAndWhite(BitmapSource source)
        {
            // Sobre fondo blanco (por si la imagen tiene transparencia) y en escala de grises.
            var flattened = new DrawingVisual();
            using (var dc = flattened.RenderOpen())
            {
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, source.PixelWidth, source.PixelHeight));
                dc.DrawImage(source, new Rect(0, 0, source.PixelWidth, source.PixelHeight));
            }
            var rendered = new RenderTargetBitmap(source.PixelWidth, source.PixelHeight, 96, 96, PixelFormats.Pbgra32);
            rendered.Render(flattened);

            var gray = new FormatConvertedBitmap(rendered, PixelFormats.Gray8, null, 0);
            var stride = gray.PixelWidth;
            var pixels = new byte[stride * gray.PixelHeight];
            gray.CopyPixels(pixels, stride, 0);

            // Umbral: lo oscuro (fondo azul marino, barras) queda negro; lo claro (texto, cian, caja del código) queda blanco.
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = pixels[i] < 128 ? (byte)0 : (byte)255;

            var result = BitmapSource.Create(gray.PixelWidth, gray.PixelHeight, 96, 96, PixelFormats.Gray8, null, pixels, stride);
            result.Freeze();
            return result;
        }
    }
}
