using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIFFA.Core.Models;
using SIFFA.Core.Models.Custom.Request.ManagementPlattform;
using SIFFA.Core.Services.EncryptionService;
using SIFFA.Core.Services.ManagementPlattformService;
using System.Text;

// Consola de configuración inicial de SIFFA: si el usuario administrador todavía no tiene credenciales,
// permite crearlas (activar el sistema).
//
// Uso:  SIFFA.AdminConsole [--config <ruta a un appsettings.json>] [--connection "<cadena>"]
// La configuración se toma, en este orden: appsettings.json junto al ejecutable, --config, variables de
// entorno (ConnectionStrings__DefaultConnection, Seed__AdminPassword) y --connection.

Console.OutputEncoding = Encoding.UTF8;

var builder = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true);

var configPath = ArgValue(args, "--config");
if (configPath is not null)
{
    if (!File.Exists(configPath))
    {
        Fail($"No existe el archivo de configuración: {configPath}");
        return 1;
    }
    builder.AddJsonFile(Path.GetFullPath(configPath), optional: false);
}

builder.AddEnvironmentVariables();

var connectionArg = ArgValue(args, "--connection");
if (connectionArg is not null)
    builder.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = connectionArg });

var configuration = builder.Build();

Banner();

var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("No hay cadena de conexión configurada.");
    connectionString = Ask("Cadena de conexión a SQL Server");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Fail("Se necesita una cadena de conexión para continuar.");
        return 1;
    }
}

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddDbContext<SiffaContext>(o => o.UseSqlServer(connectionString));
services.AddSingleton<IEncryptionService, EncryptionService>();
services.AddScoped<IManagementPlattform, ManagementPlattform>();

await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var platform = scope.ServiceProvider.GetRequiredService<IManagementPlattform>();

// 1) ¿Ya está configurado?
bool activated;
try
{
    Info("Verificando el estado del sistema…");
    activated = await platform.IsSystemActivated();
}
catch (Exception ex)
{
    Fail($"No se pudo consultar la base de datos: {ex.GetBaseException().Message}");
    return 1;
}

if (activated)
{
    Ok("El sistema ya está configurado: el usuario administrador tiene credenciales. No hay nada que hacer.");
    return 0;
}

// 2) Configurarlo
Console.WriteLine();
Console.WriteLine("El usuario administrador todavía no está configurado.");
Console.WriteLine("Necesitas la clave de activación (Seed:AdminPassword) y la contraseña que tendrá el administrador.");
Console.WriteLine();

var activationKey = AskSecret("Clave de activación");
string password;
while (true)
{
    password = AskSecret("Contraseña del administrador");
    if (password.Length < 8)
    {
        Fail("La contraseña debe tener al menos 8 caracteres.");
        continue;
    }
    if (password != AskSecret("Confirma la contraseña"))
    {
        Fail("Las contraseñas no coinciden.");
        continue;
    }
    break;
}

var result = await platform.ActivateSystem(new ActivateSystemRequest
{
    SeedActivation = activationKey,
    PasswordAdmin = password,
});

if (!result.IsSuccess)
{
    Fail(result.Message);
    return 1;
}

Ok(result.Message);
Console.WriteLine("Ya puedes iniciar sesión con adminsiffa@multicodedev.com y la contraseña que acabas de definir.");
return 0;

// ---------- utilidades ----------

static string? ArgValue(string[] args, string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static string Ask(string label)
{
    Console.Write($"{label}: ");
    return (Console.ReadLine() ?? "").Trim();
}

// Lee sin mostrar lo que se escribe.
static string AskSecret(string label)
{
    Console.Write($"{label}: ");
    if (Console.IsInputRedirected)
        return Console.ReadLine() ?? "";

    var sb = new StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
            break;
        if (key.Key == ConsoleKey.Backspace)
        {
            if (sb.Length > 0) sb.Length--;
        }
        else if (!char.IsControl(key.KeyChar))
        {
            sb.Append(key.KeyChar);
        }
    }
    Console.WriteLine();
    return sb.ToString();
}

static void Banner()
{
    Colored("SIFFA · Configuración inicial", ConsoleColor.Cyan);
    Colored(new string('─', 32), ConsoleColor.DarkGray);
}

static void Info(string text) => Colored(text, ConsoleColor.Gray);
static void Ok(string text) => Colored("✔ " + text, ConsoleColor.Green);
static void Fail(string text) => Colored("✘ " + text, ConsoleColor.Red);

static void Colored(string text, ConsoleColor color)
{
    var previous = Console.ForegroundColor;
    Console.ForegroundColor = color;
    Console.WriteLine(text);
    Console.ForegroundColor = previous;
}
