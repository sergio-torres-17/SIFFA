using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFFA.Core.Models.Custom.Response;
using SIFFA.Core.Services.AttendanceService;

namespace SIFFA.WebApp.Controllers
{
    /// <summary>
    /// Quién está dentro, quién no ha entrado y quién ya salió.
    /// Acceso: sesión iniciada, o la cabecera <c>X-Api-Key</c> con la clave configurada en <c>Api:Key</c>
    /// (variable de entorno <c>Api__Key</c>). Sin clave configurada, solo entra con sesión.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous] // la autorización se resuelve a mano abajo para poder aceptar la clave de API
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendance;
        private readonly IConfiguration _configuration;

        public AttendanceController(IAttendanceService attendance, IConfiguration configuration)
        {
            _attendance = attendance;
            _configuration = configuration;
        }

        /// <summary>Todos los empleados con su estado. Filtro opcional: ?status=inside|notentered|exited.</summary>
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? status)
        {
            if (!IsAuthorized()) return Unauthorized(new { error = "Se requiere sesión o una clave de API válida (X-Api-Key)." });

            AttendanceStatus? filter = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!TryParse(status, out var parsed))
                    return BadRequest(new { error = "status debe ser inside, notentered o exited." });
                filter = parsed;
            }
            return Ok(await _attendance.GetReportAsync(filter));
        }

        /// <summary>Empleados que entraron y todavía no han salido.</summary>
        [HttpGet("inside")]
        public Task<IActionResult> Inside() => Get("inside");

        /// <summary>Empleados que aún no han entrado.</summary>
        [HttpGet("not-entered")]
        public Task<IActionResult> NotEntered() => Get("notentered");

        /// <summary>Empleados que ya salieron.</summary>
        [HttpGet("exited")]
        public Task<IActionResult> Exited() => Get("exited");

        private static bool TryParse(string value, out AttendanceStatus status)
        {
            switch (value.Trim().Replace("-", "").Replace("_", "").ToLowerInvariant())
            {
                case "inside": status = AttendanceStatus.Inside; return true;
                case "notentered": status = AttendanceStatus.NotEntered; return true;
                case "exited": status = AttendanceStatus.Exited; return true;
                default: status = default; return false;
            }
        }

        private bool IsAuthorized()
        {
            if (User.Identity?.IsAuthenticated == true) return true;

            var expected = _configuration["Api:Key"];
            if (string.IsNullOrEmpty(expected)) return false;
            var sent = Request.Headers["X-Api-Key"].ToString();
            return sent.Length > 0 && CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(sent)),
                SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
        }
    }
}
