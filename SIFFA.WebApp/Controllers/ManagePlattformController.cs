using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SIFFA.Core.Models.Custom.Request.ManagementPlattform;
using SIFFA.Core.Services.ManagementPlattformService;

namespace SIFFA.WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManagePlattformController : ControllerBase
    {
        public ManagePlattformController(IManagementPlattform managementPlattform)
        {
            ManagementPlattform = managementPlattform;
        }

        private IManagementPlattform ManagementPlattform { get; set; }

        // Anónimo a propósito: se usa para activar el sistema antes de que exista ninguna sesión.
        // La protección es la clave de activación (Seed:AdminPassword) que valida el servicio.
        [AllowAnonymous]
        [HttpPost("ActivateSystem")]
        public async Task<IActionResult> ActivateSystem([FromBody] ActivateSystemRequest request)
        {
            var response = await ManagementPlattform.ActivateSystem(request);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }
    }
}
