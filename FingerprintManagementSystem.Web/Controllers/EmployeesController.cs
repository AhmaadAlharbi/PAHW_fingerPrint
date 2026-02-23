using FingerprintManagementSystem.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FingerprintManagementSystem.Web.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using FingerprintManagementSystem.ApiAdapter.Persistence;
using FingerprintManagementSystem.ApiAdapter.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

[Route("employees")]
public class EmployeesController : Controller
{
    private readonly IEmployeeDevicesApi _api;
    private readonly LocalAppDbContext _db;

    
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // تشيك هل المستخدم مسجل دخول
        var isLoggedIn = !string.IsNullOrWhiteSpace(
            HttpContext.Session.GetString("EmpName")
        );

        if (!isLoggedIn)
        {
            // إذا مو مسجل دخول → رجّعه لصفحة الدخول
            context.Result = RedirectToAction("Index", "Home");
            return; // مهم
        }

        base.OnActionExecuting(context);
    }

    public EmployeesController(IEmployeeDevicesApi api,LocalAppDbContext db)
    {
        _api = api;
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? employeeId, CancellationToken ct)
    {
        if (!employeeId.HasValue || employeeId.Value <= 0)
            return View("Search", null); // صفحة البحث فقط بدون نتائج

        var screen = await _api.GetEmployeeDevicesScreenAsync(employeeId.Value, ct);
        return View("Search", screen);
    }
    [HttpGet("search")]
    public async Task<IActionResult> Search(int employeeId, CancellationToken ct)
    {
        if (employeeId <= 0) return View("Search");

        var screen = await _api.GetEmployeeDevicesScreenAsync(employeeId, ct);
        TempData["LastEmployeeId"] = employeeId.ToString();
        return View("Search", screen);
    }




    [HttpPost("search")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SearchPost([FromForm] int employeeId, CancellationToken ct)
    {
        var screen = await _api.GetEmployeeDevicesScreenAsync(employeeId, ct);
        TempData["LastEmployeeId"] = employeeId.ToString();

        if (screen == null || screen.Employee == null)
        {
            TempData["ToastType"] = "danger";
            TempData["ToastMsg"] = $"لا يوجد موظف بالرقم الوظيفي: {employeeId}";
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
    if (terminalIds == null || terminalIds.Count == 0)
    {
        TempData["ToastType"] = "danger";
        TempData["ToastMsg"] = "❌ اختر أجهزة أولاً";
        TempData["LastEmployeeId"] = employeeId.ToString();
        return RedirectToAction("Search", "Employees", new { employeeId });
    }

    if (endDate < startDate)
    {
        TempData["ToastType"] = "danger";
        TempData["ToastMsg"] = "❌ تاريخ النهاية لازم يكون بعد البداية";
        TempData["LastEmployeeId"] = employeeId.ToString();
        return RedirectToAction("Search", "Employees", new { employeeId });
    }

    // (اختياري) ضبط البداية على بداية اليوم
    startDate = startDate.Date;

    // ✅ النهاية
#if DEBUG
    endDate = DateTime.Now.AddMinutes(2); // اختبار: ينتهي بعد دقيقتين
#else
    endDate = endDate.Date.AddDays(1);    // إنتاج: نهاية اليوم المختار
#endif

    var now = DateTime.Now;

    // ✅ مهم: حدد الحالة حسب الوقت
    var status = (startDate <= now && endDate > now) ? "Active" : "Scheduled";

    var d = new Delegation
    {
        EmployeeId = employeeId,
        StartDate = startDate,
        EndDate = endDate,
        Status = status,
        CreatedAt = now,
        Terminals = terminalIds
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => new DelegationTerminal { TerminalId = t.Trim() })
            .ToList()
    };

    _db.Delegations.Add(d);
    await _db.SaveChangesAsync(ct);

    TempData["ToastType"] = "success";
    TempData["ToastMsg"] = status == "Active"
        ? $"✅ تم تفعيل انتداب {terminalIds.Count} جهاز"
        : $"✅ تم جدولة انتداب {terminalIds.Count} جهاز";

    TempData["SelectedTerminalIds"] = string.Join(",", terminalIds);
    TempData["LastEmployeeId"] = employeeId.ToString();

    // ✅ إرجاع البيانات مباشرة بدل redirect عشان يظهر التحديث فوراً
    var screen = await _api.GetEmployeeDevicesScreenAsync(employeeId, ct);
    return View("Search", screen);
}



}