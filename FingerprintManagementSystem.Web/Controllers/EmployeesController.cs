using FingerprintManagementSystem.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FingerprintManagementSystem.Web.Controllers;

[Route("employees")]
public class EmployeesController : Controller
{
    private readonly IEmployeeDevicesApi _api;

    public EmployeesController(IEmployeeDevicesApi api)
    {
        _api = api;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View("Search");
    }

    [HttpPost("search")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Search([FromForm] int employeeId, CancellationToken ct)
    {
        var screen = await _api.GetEmployeeDevicesScreenAsync(employeeId, ct);
        if (screen == null)
        {
            ViewBag.NotFound = "الموظف غير موجود";
            TempData["LastEmployeeId"] = employeeId.ToString();
            return View("Search");
        }
        return View("Search", screen);
    }

    [HttpPost("assign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int employeeId, string terminalId, CancellationToken ct)
    {
        var ok = await _api.AssignOneAsync(employeeId, terminalId, ct);
        TempData["ToastType"] = ok ? "success" : "danger";
        TempData["ToastMsg"] = ok ? "✅ تم الربط" : "❌ فشل الربط";
        TempData["LastEmployeeId"] = employeeId.ToString();

        // ✅ الإصلاح: إرجاع البيانات بدل redirect لصفحة فاضية
        var screen = await _api.GetEmployeeDevicesScreenAsync(employeeId, ct);
        return View("Search", screen);
    }

    [HttpPost("unassign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unassign(int employeeId, string terminalId, CancellationToken ct)
    {
        var ok = await _api.UnassignOneAsync(employeeId, terminalId, ct);
        TempData["ToastType"] = ok ? "success" : "danger";
        TempData["ToastMsg"] = ok ? "✅ تم فك الربط" : "❌ فشل فك الربط";
        TempData["LastEmployeeId"] = employeeId.ToString();

        // ✅ الإصلاح: إرجاع البيانات بدل redirect لصفحة فاضية
        var screen = await _api.GetEmployeeDevicesScreenAsync(employeeId, ct);
        return View("Search", screen);
    }
    [HttpPost("AssignBulk")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignBulk(int employeeId, List<string> terminalIds, CancellationToken ct)
    {
        // Logic للربط بالجملة
        foreach (var terminalId in terminalIds)
        {
            await _api.AssignOneAsync(employeeId, terminalId, ct);
        }

        TempData["ToastType"] = "success";
        TempData["ToastMsg"] = $"✅ تم ربط {terminalIds.Count} جهاز";

        var screen = await _api.GetEmployeeDevicesScreenAsync(employeeId, ct);
        return View("Search", screen);
    }

    [HttpPost("UnassignBulk")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnassignBulk(int employeeId, List<string> terminalIds, CancellationToken ct)
    {
        // Logic لفك الربط بالجملة
        foreach (var terminalId in terminalIds)
        {
            await _api.UnassignOneAsync(employeeId, terminalId, ct);
        }

        TempData["ToastType"] = "success";
        TempData["ToastMsg"] = $"✅ تم فك {terminalIds.Count} جهاز";

        var screen = await _api.GetEmployeeDevicesScreenAsync(employeeId, ct);
        return View("Search", screen);
    }

    [HttpPost("DelegateRegion")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DelegateRegion(
        int employeeId,
        List<string> terminalIds,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct)
    {
        // Logic للانتداب المؤقت
        foreach (var terminalId in terminalIds)
        {
            // تنفيذ logic الانتداب هنا
            await _api.AssignOneAsync(employeeId, terminalId, ct);
        }

        TempData["ToastType"] = "success";
        TempData["ToastMsg"] = $"✅ تم انتداب {terminalIds.Count} جهاز";

        var screen = await _api.GetEmployeeDevicesScreenAsync(employeeId, ct);
        return View("Search", screen);
    }
}