using FingerprintManagementSystem.ApiAdapter.Alpeta;
using FingerprintManagementSystem.ApiAdapter.Implementations;
using FingerprintManagementSystem.ApiAdapter.Persistence;
using FingerprintManagementSystem.ApiAdapter.Soap;
using FingerprintManagementSystem.Contracts;
using Microsoft.EntityFrameworkCore;

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

var app = builder.Build();

// ✅ Ensure DB created (اختياري، لكن OK)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LocalAppDbContext>();
    db.Database.EnsureCreated();
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Employees}/{action=Index}/{id?}");

app.Run();
