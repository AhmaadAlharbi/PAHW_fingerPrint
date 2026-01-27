using FingerprintManagementSystem.ApiAdapter.Alpeta;
using FingerprintManagementSystem.ApiAdapter.Implementations;
using FingerprintManagementSystem.ApiAdapter.Persistence;
using FingerprintManagementSystem.ApiAdapter.Soap;
using FingerprintManagementSystem.Contracts;
using Microsoft.EntityFrameworkCore;
using FingerprintManagementSystem.Web.Services;
using FingerprintManagementSystem.ApiAdapter;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// -------------------------
// ✅ Local SQLite (fix relative path)
// -------------------------
var cs = builder.Configuration.GetConnectionString("LocalDb")
         ?? "Data Source=App_Data/local.db";

if (cs.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
{
    var dbRel = cs.Substring("Data Source=".Length).Trim();
    if (!Path.IsPathRooted(dbRel))
    {
        var dbAbs = Path.Combine(builder.Environment.ContentRootPath, dbRel);
        cs = $"Data Source={dbAbs}";
    }
}

builder.Services.AddDbContext<LocalAppDbContext>(opt => opt.UseSqlite(cs));
builder.Services.AddScoped<RegionMappingService>();
builder.Services.AddHttpClient(); // مهم لأن SoapLoginApi يعتمد على HttpClient
builder.Services.AddScoped<ILoginApi, SoapLoginApi>();
builder.Services.AddSession();
builder.Services.AddScoped<IAllowedUsersStore, SqliteAllowedUsersStore>();
builder.Services.AddScoped<IAllowedUsersAdmin, AllowedUsersAdminService>();
// -------------------------
// ✅ SOAP client
// -------------------------
builder.Services.AddScoped<EmployeeSoapClient>();

// -------------------------
// ✅ Alpeta client (HttpClient)
// -------------------------
builder.Services.AddHttpClient<AlpetaClient>(http =>
{
    // إذا تحتاج Timeout من config:
    // http.Timeout = TimeSpan.FromSeconds(10);
});

// -------------------------
// ✅ Facade API
// -------------------------
builder.Services.AddScoped<IEmployeeDevicesApi, EmployeeDevicesApi>();
builder.Services.AddHostedService<DelegationWorker>();

var app = builder.Build();

// ✅ Ensure DB created (اختياري، لكن OK)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LocalAppDbContext>();
    db.Database.Migrate();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
