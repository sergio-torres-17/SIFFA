using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SIFFA.Core.Models;
using SIFFA.Core.Security;
using SIFFA.Core.Models.Custom;
using SIFFA.Core.Models.Custom.Request.ManagementPlattform;
using SIFFA.Core.Services.EncryptionService;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SIFFA.Core.Services.ManagementPlattformService
{
    public class ManagementPlattform: IManagementPlattform
    {
        public ManagementPlattform(SiffaContext db, IEncryptionService encryptionService, IConfiguration configuration)
        {
            Db = db;
            EncryptionService = encryptionService;
            Configuration = configuration;
        }

        private SiffaContext Db { get; set; }
        private IEncryptionService EncryptionService { get; set; }
        private IConfiguration Configuration { get; set; }

        public async Task<GenericReponse> ActivateSystem(ActivateSystemRequest request)
        {
            // La clave de activación (SeedActivation) debe ser el Seed:AdminPassword del appsettings.
            var activationKey = Configuration["Seed:AdminPassword"];
            if (string.IsNullOrWhiteSpace(activationKey))
                return new GenericReponse(false, "No hay una clave de activación configurada.");

            if (!SecretEquals(request.SeedActivation, activationKey))
                return new GenericReponse(false, "Clave de activación inválida.");

            // La contraseña con la que el administrador iniciará sesión es la que llega en la solicitud.
            if (string.IsNullOrWhiteSpace(request.PasswordAdmin))
                return new GenericReponse(false, "La contraseña del administrador es obligatoria.");

            // El usuario administrador lo crea el script postdeployment (Security.Usp_Insert_Basics).
            const string adminEmail = "adminsiffa@multicodedev.com";

            var user = await Db.Users.FirstOrDefaultAsync(u => u.Email.Equals(adminEmail));
            if (user is null)
                return new GenericReponse(false, "No existe el usuario administrador. Publica la base de datos para crearlo.");

            //Verify if exist login for Main Admin user
            var hasCredential = await Db.Credentials.AnyAsync(c => c.UserId == user.UserId);
            if (hasCredential)
                return new GenericReponse(false, "El sistema ya está activado.");

            Db.Credentials.Add(new Credential
            {
                CredentialId = Guid.NewGuid(),
                UserId = user.UserId,
                UsernameHash = EncryptionService.HashPassword(user.Email),
                PasswordHash = EncryptionService.HashPassword(request.PasswordAdmin),
                CreatedAt = DateTime.Now,
            });
            await Db.SaveChangesAsync();

            return new GenericReponse(true, "Sistema activado correctamente.");
        }

        // Comparación en tiempo constante, para no filtrar la clave por el tiempo de respuesta.
        private static bool SecretEquals(string? provided, string expected)
        {
            if (provided is null)
                return false;

            return CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(provided)),
                SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
        }
    }
}
