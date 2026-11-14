using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using SIFFA.Core.Models;
using SIFFA.Core.Security;
using SIFFA.Core.Services.EncryptionService;
using SIFFA.Core.Services.EmployeeService;
using SIFFA.WebApp.Components;
using SIFFA.Core.Services.ManagementPlattformService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddControllers();

builder.Services.AddDbContext<SiffaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IManagementPlattform, ManagementPlattform>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = "SIFFA.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Todo requiere sesión, salvo lo marcado con [AllowAnonymous] (p. ej. /login).
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets().AllowAnonymous();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapControllers();

app.MapPost("/account/login", async (
    HttpContext http,
    SiffaContext db,
    IEncryptionService encryption,
    [FromForm] string? username,
    [FromForm] string? password,
    [FromForm] string? returnUrl) =>
{
    var safeReturn = IsLocalUrl(returnUrl) ? returnUrl! : "/";
    var usernameHash = encryption.HashPassword(username ?? string.Empty);

    var credential = await db.Credentials.AsNoTracking()
        .Include(c => c.User)
        .FirstOrDefaultAsync(c =>c.User.Email.ToLower().Equals(username));

    if (!encryption.VerifyPassword(password ?? string.Empty, credential?.PasswordHash) || credential is null)
    {
        return Results.Redirect($"/login?error=1&returnUrl={Uri.EscapeDataString(safeReturn)}");
    }

    var user = credential.User;
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
        new(ClaimTypes.Email, user.Email),
    };
    var principal = new ClaimsPrincipal(
        new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

    return Results.LocalRedirect(safeReturn);
}).AllowAnonymous();

app.MapMethods("/account/logout", new[] { "GET", "POST" }, async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.Run();

static bool IsLocalUrl(string? url) =>
    !string.IsNullOrEmpty(url)
    && url[0] == '/'
    && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
