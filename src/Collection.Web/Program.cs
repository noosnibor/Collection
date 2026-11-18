using Collection.Data;
using Collection.Library.Repository;
using Collection.Library.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Collection.Web.Components;
using Collection.Web.Components.Custom.Toast;
using Collection.Web.Components.Services;

// Helper for setting up the System Admin:  dotnet Collection.Web.dll --hash-password "your password"
// Prints the BCrypt hash to put in the SystemAdmin__PasswordHash setting, then exits (the web app is not started).
if (args.Length == 2 && args[0] == "--hash-password")
{
    Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(args[1]));
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Built-in System Admin (configured on the server, not stored in the database).
builder.Services.Configure<SystemAdminOptions>(builder.Configuration.GetSection(SystemAdminOptions.SectionName));

#region Blazor

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    // Snapshot sends each few seconds of recording from the browser to the server in one message.
    // SignalR's default limit is 32 KB; a bigger message silently drops the connection, so nothing was ever saved.
    .AddHubOptions(options => options.MaximumReceiveMessageSize = 4 * 1024 * 1024);

#endregion

#region Data access and repositories

builder.Services.AddScoped<IDataAccess, DataAccess>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();
builder.Services.AddScoped<ICollectionTypeRepository, CollectionTypeRepository>();
builder.Services.AddScoped<IPaymentTypeRepository, PaymentTypeRepository>();
builder.Services.AddScoped<IMemberTypeRepository, MemberTypeRepository>();
builder.Services.AddScoped<ICollectionRepository, CollectionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IPeriodRepository, PeriodRepository>();
builder.Services.AddScoped<ISnapshotRepository, SnapshotRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();

#endregion

#region Authentication and authorization

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationService>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizarionHandlerService>();

// One policy per permission name; each policy requires that permission claim.
builder.Services.AddAuthorizationCore(options =>
{
    var permissions = StaticDataService
        .GetPermissions()
        .Select(x => x.Name)
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .Distinct();

    foreach (var permission in permissions)
    {
        options.AddPolicy(permission!, policy =>
            policy.Requirements.Add(new PermissionRequirementService(permission!)));
    }
});

#endregion

#region Application services

builder.Services.AddScoped(typeof(ILogService<>), typeof(LogService<>));
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddScoped<AppVersionService>();
builder.Services.AddScoped<ChangePasswordMoveableObject>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<LocalTimeService>();  // shows stored UTC times in the viewer's own time zone
builder.Services.AddScoped<ReportExportService>();

// Snapshot: removes old recordings once a day (does nothing while Snapshot:Enabled is false).
builder.Services.AddHostedService<SnapshotRetentionService>();

// QuestPDF licence – must be set once at startup (Community licence).
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

#endregion

var app = builder.Build();

#region HTTP pipeline

if (!app.Environment.IsDevelopment())
{
    // Unhandled errors render /Error (Components/Pages/Error.razor).
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    // Default HSTS is 30 days; revisit for production (https://aka.ms/aspnetcore-hsts).
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

#endregion

app.Run();
