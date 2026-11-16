using Velopack;

namespace SIFFA.DesktopApp
{
    public static class Program
    {
        // Punto de entrada propio (ver StartupObject en el .csproj).
        // Velopack debe ejecutarse lo primero: atiende los argumentos que le pasa el instalador/actualizador
        // (instalar, actualizar, desinstalar) y en esos casos cierra el proceso antes de abrir ninguna ventana.
        [STAThread]
        public static void Main(string[] args)
        {
            VelopackApp.Build().Run();

            var app = new App();
            app.Run();
        }
    }
}
