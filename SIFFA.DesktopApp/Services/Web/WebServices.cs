using System.IO;
using SIFFA.Core.Models;
using SIFFA.Core.Models.Custom;
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

// Implementaciones de los servicios del Core que, en lugar de usar SQL Server, llaman a la API de la consola web.
// El escritorio las usa cuando el equipo es "cliente de consola web"; las pantallas no notan la diferencia.
namespace SIFFA.DesktopApp.Services.Web
{
    public class WebDashboardService : IDashboardService
    {
        private readonly WebApi _api;
        public WebDashboardService(WebApi api) => _api = api;

        public Task<DashboardSummary> GetSummaryAsync(int latestCount = 10) =>
            _api.GetAsync<DashboardSummary>($"dashboard?latest={latestCount}");
    }

    public class WebAttendanceService : IAttendanceService
    {
        private readonly WebApi _api;
        public WebAttendanceService(WebApi api) => _api = api;

        public Task<AttendanceReport> GetReportAsync(AttendanceStatus? status = null) =>
            _api.GetAsync<AttendanceReport>(status is null ? "attendance" : $"attendance?status={status}");
    }

    public class WebAccessControlService : IAccessControlService
    {
        private readonly WebApi _api;
        public WebAccessControlService(WebApi api) => _api = api;

        // Los errores de conexión se devuelven como respuesta fallida: las pantallas ya muestran el mensaje.
        public AddTraceResponse AddTrace(InsertTraceAccessControl request)
        {
            try { return _api.Post<AddTraceResponse>("access/trace", request); }
            catch (WebApiException ex) { return new AddTraceResponse(false, ex.Message, null); }
        }

        public bool ValidateTrace(InsertTraceAccessControl request) =>
            throw new NotSupportedException("ValidateTrace no se usa con la consola web.");

        public List<EmployeeSearchResult> SearchEmployees(string term) =>
            _api.Get<List<EmployeeSearchResult>>($"employees/search?term={Uri.EscapeDataString(term ?? "")}");

        public GenericReponse SaveDeliveries(SaveDeliveriesRequest request)
        {
            try { return _api.Post<GenericReponse>("access/deliveries", request); }
            catch (WebApiException ex) { return new GenericReponse(false, ex.Message); }
        }
    }

    public class WebAddTypeService : IAddTypeService
    {
        private sealed record Body(string? Name, string? Description);

        private readonly WebApi _api;
        public WebAddTypeService(WebApi api) => _api = api;

        public Task<List<AddTypeItem>> GetAllAsync() => _api.GetAsync<List<AddTypeItem>>("add-types");

        public Task<GenericReponse> CreateAsync(string name, string? description) =>
            _api.PostAsync<GenericReponse>("add-types", new Body(name, description));

        public Task<GenericReponse> UpdateAsync(Guid addTypeId, string name, string? description) =>
            _api.PutAsync<GenericReponse>($"add-types/{addTypeId}", new Body(name, description));

        public Task<GenericReponse> DeleteAsync(Guid addTypeId) => _api.DeleteAsync<GenericReponse>($"add-types/{addTypeId}");
    }

    public class WebUserAccountService : IUserAccountService
    {
        private sealed record NewUser(string FirstName, string LastName, string Email, string Password);

        private readonly WebApi _api;
        public WebUserAccountService(WebApi api) => _api = api;

        public Task<List<UserAccountItem>> GetAllAsync() => _api.GetAsync<List<UserAccountItem>>("users");

        public Task<GenericReponse> CreateAsync(string firstName, string lastName, string email, string password) =>
            _api.PostAsync<GenericReponse>("users", new NewUser(firstName, lastName, email, password));

        public Task<GenericReponse> ResetPasswordAsync(Guid userId, string newPassword) =>
            _api.PostAsync<GenericReponse>($"users/{userId}/password", new { password = newPassword });

        // El servidor toma el usuario actual del token; currentUserId se ignora.
        public Task<GenericReponse> DeleteAsync(Guid userId, Guid? currentUserId) =>
            _api.DeleteAsync<GenericReponse>($"users/{userId}");
    }

    public class WebEmployeeService : IEmployeeService
    {
        private sealed record CatalogItem(Guid AddTypeId, string AddTypeName, string? AddTypeDescription);

        private readonly WebApi _api;
        public WebEmployeeService(WebApi api) => _api = api;

        public GenericReponse AddSeveralEmployees(List<InsertEmployeeRequest> employees) =>
            throw new NotSupportedException("AddSeveralEmployees no se usa con la consola web.");

        public async Task<List<AddType>> GetAddTypesAsync() =>
            (await _api.GetAsync<List<CatalogItem>>("add-types/catalog"))
                .Select(a => new AddType { AddTypeId = a.AddTypeId, AddTypeName = a.AddTypeName, AddTypeDescription = a.AddTypeDescription })
                .ToList();

        public async Task<GenericReponse> RegisterEmployeeImportAsync(EmployeeImportRequest request)
        {
            try { return await _api.PostAsync<GenericReponse>("employees/import", request); }
            catch (WebApiException ex) { return new GenericReponse(false, ex.Message); }
        }

        public Task<InvitationCardData?> GetCardAsync(string employeeNumber) =>
            _api.GetOrNullAsync<InvitationCardData>($"cards/{Uri.EscapeDataString(employeeNumber)}");

        public Task<List<InvitationCardData>> GetAllCardsAsync() => _api.GetAsync<List<InvitationCardData>>("cards");

        public Task<InvitationCardData?> GetCardSampleAsync() => _api.GetOrNullAsync<InvitationCardData>("cards/sample");

        // El dibujo es local: solo los datos de los empleados vienen de la consola web.
        public byte[]? RenderCardPreview(InvitationCardData card, byte[] logo, CardCodeType code = CardCodeType.Qr)
        {
            using var bitmap = InvitationCardRenderer.DecodeLogo(logo);
            return bitmap is null ? null : InvitationCardRenderer.RenderPng(card, bitmap, code);
        }

        public async Task<CardsZipResult> GenerateCardsZipAsync(byte[] logo, CardFormat format, CardCodeType code,
            Stream output, IProgress<CardsProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var cards = await GetAllCardsAsync();
                return await CardsZipWriter.WriteAsync(cards, logo, format, code, output, progress, cancellationToken);
            }
            catch (WebApiException ex)
            {
                return new CardsZipResult(false, ex.Message);
            }
        }
    }
}
