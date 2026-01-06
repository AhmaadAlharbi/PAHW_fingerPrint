using FingerprintManagementSystem.ApiAdapter.Alpeta;
using FingerprintManagementSystem.ApiAdapter.Implementations;
using FingerprintManagementSystem.ApiAdapter.Persistence;
using FingerprintManagementSystem.ApiAdapter.Soap;
using FingerprintManagementSystem.Contracts;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// ================================
// SQLite Local DB (Regions Mapping)
// ================================
builder.Services.AddDbContext<LocalAppDbContext>(options =>
{
    // تخزين ملف SQLite داخل مشروع Web (App_Data)
    var dbPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "local.db");
    Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

    options.UseSqlite($"Data Source={dbPath}");
});

// ================================
// HttpClient لكل Client
// ================================
builder.Services.AddHttpClient<AlpetaClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Alpeta:BaseUrl"] ?? "http://192.168.120.56:9004/v1");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<EmployeeSoapClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ================================
// Services
// ================================
builder.Services.AddScoped<IAttendanceApi, AttendanceWithDevicesApi>();
builder.Services.AddScoped<RegionMappingService>();
// Alpeta typed client
builder.Services.AddHttpClient<AlpetaClient>();

// Employee + Devices facade
builder.Services.AddScoped<IEmployeeDevicesApi, EmployeeDevicesApi>();

var app = builder.Build();

// ================================
// إنشاء DB تلقائياً عند التشغيل
// ================================
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<LocalAppDbContext>();
//    db.Database.Migrate();
//}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
