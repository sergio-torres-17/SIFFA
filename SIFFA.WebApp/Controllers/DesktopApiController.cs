using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIFFA.Core.Models.Custom.Model;
using SIFFA.Core.Models.Custom.Request.AccessControl;
using SIFFA.Core.Models.Custom.Request.Employeees;
using SIFFA.Core.Models.Custom.Response;
using SIFFA.Core.Services.AccessControlService;
using SIFFA.Core.Services.AddTypeService;
using SIFFA.Core.Services.AttendanceService;
using SIFFA.Core.Services.DashboardService;
using SIFFA.Core.Services.EmployeeService;
using SIFFA.Core.Services.UserAccountService;
using SIFFA.WebApp.Security;

namespace SIFFA.WebApp.Controllers
{
    public record AddTypeBody(string? Name, string? Description);
    public record NewUserBody(string? FirstName, string? LastName, string? Email, string? Password);
    public record PasswordBody(string? Password);
    public record AddTypeCatalogItem(Guid AddTypeId, string AddTypeName, string? AddTypeDescription);

    /// <summary>
    /// API para el escritorio en modo "cliente de consola web": ofrece las mismas operaciones que usa el escritorio
    /// cuando habla directo con SQL Server. Requiere el token que entrega api/Auth/Login (Authorization: Bearer).
    /// </summary>
    [Route("api/desktop")]
    [ApiController]
    [Authorize(AuthenticationSchemes = DesktopTokenService.Scheme)]
    public class DesktopApiController : ControllerBase
    {
        private readonly IDashboardService _dashboard;
        private readonly IAttendanceService _attendance;
        private readonly IAccessControlService _access;
        private readonly IEmployeeService _employees;
        private readonly IAddTypeService _addTypes;
        private readonly IUserAccountService _users;

        public DesktopApiController(IDashboardService dashboard, IAttendanceService attendance, IAccessControlService access,
            IEmployeeService employees, IAddTypeService addTypes, IUserAccountService users)
        {
            _dashboard = dashboard;
            _attendance = attendance;
            _access = access;
            _employees = employees;
            _addTypes = addTypes;
            _users = users;
        }

        // ---------- dashboard y asistencia ----------

        [HttpGet("dashboard")]
        public async Task<DashboardSummary> Dashboard([FromQuery] int latest = 10) =>
            await _dashboard.GetSummaryAsync(Math.Clamp(latest, 1, 100));

        [HttpGet("attendance")]
        public async Task<AttendanceReport> Attendance([FromQuery] AttendanceStatus? status) =>
            await _attendance.GetReportAsync(status);

        // ---------- control de accesos ----------

        [HttpGet("employees/search")]
        public List<EmployeeSearchResult> Search([FromQuery] string? term) => _access.SearchEmployees(term ?? "");

        [HttpPost("access/trace")]
        public AddTraceResponse Trace([FromBody] InsertTraceAccessControl request) => _access.AddTrace(request);

        [HttpPost("access/deliveries")]
        public SIFFA.Core.Models.Custom.GenericReponse Deliveries([FromBody] SaveDeliveriesRequest request) =>
            _access.SaveDeliveries(request);

        // ---------- invitaciones y carga de empleados ----------

        [HttpGet("cards")]
        public async Task<List<InvitationCardData>> Cards() => await _employees.GetAllCardsAsync();

        [HttpGet("cards/sample")]
        public async Task<IActionResult> CardSample()
        {
            var card = await _employees.GetCardSampleAsync();
            return card is null ? NotFound() : Ok(card);
        }

        [HttpGet("cards/{employeeNumber}")]
        public async Task<IActionResult> Card(string employeeNumber)
        {
            var card = await _employees.GetCardAsync(employeeNumber);
            return card is null ? NotFound() : Ok(card);
        }

        [HttpGet("add-types/catalog")]
        public async Task<List<AddTypeCatalogItem>> AddTypeCatalog() =>
            (await _employees.GetAddTypesAsync())
                .Select(a => new AddTypeCatalogItem(a.AddTypeId, a.AddTypeName, a.AddTypeDescription))
                .ToList();

        [HttpPost("employees/import")]
        public async Task<SIFFA.Core.Models.Custom.GenericReponse> Import([FromBody] EmployeeImportRequest request) =>
            await _employees.RegisterEmployeeImportAsync(request);

        // ---------- elementos adicionales ----------

        [HttpGet("add-types")]
        public async Task<List<AddTypeItem>> AddTypes() => await _addTypes.GetAllAsync();

        [HttpPost("add-types")]
        public async Task<SIFFA.Core.Models.Custom.GenericReponse> CreateAddType([FromBody] AddTypeBody body) =>
            await _addTypes.CreateAsync(body.Name ?? "", body.Description);

        [HttpPut("add-types/{id:guid}")]
        public async Task<SIFFA.Core.Models.Custom.GenericReponse> UpdateAddType(Guid id, [FromBody] AddTypeBody body) =>
            await _addTypes.UpdateAsync(id, body.Name ?? "", body.Description);

        [HttpDelete("add-types/{id:guid}")]
        public async Task<SIFFA.Core.Models.Custom.GenericReponse> DeleteAddType(Guid id) => await _addTypes.DeleteAsync(id);

        // ---------- usuarios de acceso ----------

        [HttpGet("users")]
        public async Task<List<UserAccountItem>> Users() => await _users.GetAllAsync();

        [HttpPost("users")]
        public async Task<SIFFA.Core.Models.Custom.GenericReponse> CreateUser([FromBody] NewUserBody body) =>
            await _users.CreateAsync(body.FirstName ?? "", body.LastName ?? "", body.Email ?? "", body.Password ?? "");

        [HttpPost("users/{id:guid}/password")]
        public async Task<SIFFA.Core.Models.Custom.GenericReponse> ResetPassword(Guid id, [FromBody] PasswordBody body) =>
            await _users.ResetPasswordAsync(id, body.Password ?? "");

        [HttpDelete("users/{id:guid}")]
        public async Task<SIFFA.Core.Models.Custom.GenericReponse> DeleteUser(Guid id)
        {
            // El usuario que borra es el del token, no uno que mande el cliente.
            Guid? current = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var me) ? me : null;
            return await _users.DeleteAsync(id, current);
        }
    }
}
