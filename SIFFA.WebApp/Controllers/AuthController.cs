using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFFA.Core.Services.AuthService;

namespace SIFFA.WebApp.Controllers
{
    public record LoginRequest(string? Username, string? Password);

    /// <summary>API para que los clientes de escritorio validen su login contra esta consola web.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public AuthController(IAuthService authService)
        {
            AuthService = authService;
        }

        private IAuthService AuthService { get; set; }

        // Anónimo a propósito: es el propio login. No crea sesión en el servidor; solo responde si las credenciales son válidas.
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await AuthService.ValidateAsync(request.Username, request.Password);
            return result.IsSuccess ? Ok(result) : Unauthorized(result);
        }

        // Permite al escritorio comprobar que la dirección configurada es una consola SIFFA alcanzable.
        [AllowAnonymous]
        [HttpGet("Ping")]
        public IActionResult Ping() => Ok(new { ok = true, app = "SIFFA" });
    }
}
