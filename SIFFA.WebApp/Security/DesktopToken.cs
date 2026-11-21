using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace SIFFA.WebApp.Security
{
    /// <summary>
    /// Credencial que recibe un escritorio al iniciar sesión contra esta consola. Es un token firmado y con
    /// vencimiento (Data Protection); no se guarda nada en el servidor.
    /// </summary>
    public class DesktopTokenService
    {
        public const string Scheme = "Desktop";
        public static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);

        private readonly ITimeLimitedDataProtector _protector;

        public DesktopTokenService(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("SIFFA.DesktopToken.v1").ToTimeLimitedDataProtector();
        }

        public string Create(Guid userId, string name, string email) =>
            _protector.Protect($"{userId:N}\n{name}\n{email}", Lifetime);

        public ClaimsPrincipal? Read(string token)
        {
            try
            {
                var parts = _protector.Unprotect(token).Split('\n');
                if (parts.Length != 3) return null;
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.ParseExact(parts[0], "N").ToString()),
                    new Claim(ClaimTypes.Name, parts[1]),
                    new Claim(ClaimTypes.Email, parts[2]),
                };
                return new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme));
            }
            catch
            {
                return null; // token alterado, vencido o de otra instalación
            }
        }
    }

    /// <summary>Autentica las llamadas del escritorio con la cabecera <c>Authorization: Bearer &lt;token&gt;</c>.</summary>
    public class DesktopTokenHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly DesktopTokenService _tokens;

        public DesktopTokenHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
            UrlEncoder encoder, DesktopTokenService tokens) : base(options, logger, encoder)
        {
            _tokens = tokens;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var header = Request.Headers.Authorization.ToString();
            if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(AuthenticateResult.NoResult());

            var principal = _tokens.Read(header["Bearer ".Length..].Trim());
            return Task.FromResult(principal is null
                ? AuthenticateResult.Fail("Token inválido o vencido.")
                : AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}
