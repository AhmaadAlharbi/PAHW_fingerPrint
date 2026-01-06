using FingerprintManagementSystem.ApiAdapter.Alpeta;
using FingerprintManagementSystem.ApiAdapter.Persistence;
using FingerprintManagementSystem.Contracts;
using FingerprintManagementSystem.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FingerprintManagementSystem.Web.Controllers;

[Route("employees")]
public class EmployeesController : Controller
{
    private readonly IAttendanceApi _api;
    private readonly AlpetaClient _alpeta;
    private readonly LocalAppDbContext _db;
    private readonly ILogger<EmployeesController> _log;

    // 👇 تقدر تغيّرها حسب تحمل سيرفر Alpeta
    private const int BulkMaxConcurrency = 6;

    public EmployeesController(
        IAttendanceApi api,
        AlpetaClient alpeta,
        LocalAppDbContext db,
        ILogger<EmployeesController> log)
    {
        _api = api;
        _alpeta = alpeta;
        _db = db;
        _log = log;
    }

    // GET /employees
    [HttpGet("")]
    public IActionResult Index()
    {
        return View("Search", new EmployeeDevicesViewModel());
    }

    // POST /employees/search
    [HttpPost("search")]
    public async Task<IActionResult> Search(int employeeId, CancellationToken ct)
    {
        var vm = new EmployeeDevicesViewModel();

        if (employeeId <= 0)
        {
            vm.ErrorMessage = "يرجى إدخال رقم وظيفي صحيح.";
            return View("Search", vm);
        }

        try
        {
            var result = await _api.GetEmployeeWithDevicesAsync(employeeId, ct);

            if (result == null)
            {
                ViewBag.NotFound = $"لم يتم العثور على موظف بالرقم: {employeeId}";
                return View("Search", new EmployeeDevicesViewModel());
            }

            // تجهيز البيانات الأساسية
            vm.Employee = result.Employee;

            // خريطة الأجهزة المرتبطة
            var assignedIds = (result.AssignedDevices ?? new List<FingerprintManagementSystem.Contracts.DTOs.DeviceDto>())
                .Select(d => d.DeviceId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // جلب خريطة المناطق من قاعدة البيانات المحلية
            var regionMap = await (
                from m in _db.TerminalRegionMaps.AsNoTracking()
                join r in _db.Regions.AsNoTracking() on m.RegionId equals r.Id
                select new { m.TerminalId, RegionId = (int?)r.Id, RegionName = r.Name }
            ).ToDictionaryAsync(x => x.TerminalId, x => (x.RegionId, x.RegionName), StringComparer.OrdinalIgnoreCase, ct);

            // تحويل الأجهزة إلى الـ ViewModel
            var devices = (result.AllDevices ?? new List<FingerprintManagementSystem.Contracts.DTOs.DeviceDto>())
                .Select(d =>
                {
                    var hasRegion = regionMap.TryGetValue(d.DeviceId, out var reg);
                    return new DeviceRowVm
                    {
                        DeviceId = d.DeviceId,
                        DeviceName = d.DeviceName,
                        Location = d.Location,
                        IsAssigned = !string.IsNullOrWhiteSpace(d.DeviceId) && assignedIds.Contains(d.DeviceId),
                        RegionId = hasRegion ? reg.RegionId : null,
                        RegionName = hasRegion ? reg.RegionName : null
                    };
                }).ToList();

            vm.Devices = devices;

            // تجميع الأجهزة حسب المنطقة للعرض في الـ Accordion
            vm.RegionGroups = devices
                .GroupBy(d => new
                {
                    d.RegionId,
                    RegionName = string.IsNullOrWhiteSpace(d.RegionName) ? "أجهزة غير مصنفة" : d.RegionName
                })
                .OrderBy(g => g.Key.RegionName == "أجهزة غير مصنفة" ? 1 : 0)
                .ThenBy(g => g.Key.RegionName)
                .Select(g => new RegionGroupVm
                {
                    RegionId = g.Key.RegionId,
                    RegionName = g.Key.RegionName,
                    TotalDevices = g.Count(),
                    AssignedDevices = g.Count(x => x.IsAssigned),
                    Devices = g.OrderByDescending(x => x.IsAssigned)
                               .ThenBy(x => int.TryParse(x.DeviceId, out var n) ? n : int.MaxValue)
                               .ToList()
                })
                .ToList();

            return View("Search", vm);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Search failed for EmployeeId={EmployeeId}", employeeId);
            vm.ErrorMessage = "صار خطأ أثناء جلب البيانات. تحقق من اتصال الشبكة.";
            return View("Search", vm);
        }
    }

    // POST /employees/AssignBulk
    [HttpPost("AssignBulk")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignBulk(int employeeId, string[] terminalIds, CancellationToken ct)
    {
        var ids = NormalizeTerminalIds(terminalIds);

        if (employeeId <= 0 || ids.Count == 0)
        {
            TempData["Msg"] = "لم يتم اختيار أي أجهزة للربط.";
            return RedirectToAction(nameof(Index), new { employeeId });
        }

        try
        {
            // 🔥 throttle بدل Task.WhenAll على كل الأجهزة مرة وحدة
            var (ok, fail) = await RunThrottled(ids, BulkMaxConcurrency, id =>
                _alpeta.AssignUserToTerminalAsync(id, employeeId, ct), ct);

            TempData["Msg"] = fail == 0
                ? $"✅ تم ربط {ok} أجهزة بنجاح."
                : $"⚠️ تم ربط {ok} أجهزة، وفشل {fail} جهاز.";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Bulk Assign failed for employeeId={employeeId}", employeeId);
            TempData["Msg"] = "❌ حدث خطأ أثناء عملية الربط الجماعي.";
        }

        return await Search(employeeId, ct);
    }

    // POST /employees/UnassignBulk
    [HttpPost("UnassignBulk")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnassignBulk(int employeeId, string[] terminalIds, CancellationToken ct)
    {
        var ids = NormalizeTerminalIds(terminalIds);

        if (employeeId <= 0 || ids.Count == 0)
        {
            TempData["Msg"] = "لم يتم اختيار أي أجهزة لإلغاء الربط.";
            return RedirectToAction(nameof(Index), new { employeeId });
        }

        try
        {
            var (ok, fail) = await RunThrottled(ids, BulkMaxConcurrency, id =>
                _alpeta.UnassignUserFromTerminalAsync(id, employeeId, ct), ct);

            TempData["Msg"] = fail == 0
                ? $"✅ تم إلغاء ربط {ok} أجهزة بنجاح."
                : $"⚠️ تم إلغاء ربط {ok} أجهزة، وفشل {fail} جهاز.";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Bulk Unassign failed for employeeId={employeeId}", employeeId);
            TempData["Msg"] = "❌ حدث خطأ أثناء إلغاء الربط الجماعي.";
        }

        return await Search(employeeId, ct);
    }

    // ✅ NEW: POST /employees/AssignRegion  (ربط كل أجهزة منطقة واحدة)
    [HttpPost("AssignRegion")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRegion(int employeeId, int regionId, CancellationToken ct)
    {
        if (employeeId <= 0 || regionId <= 0)
        {
            TempData["Msg"] = "بيانات غير صحيحة.";
            return await Search(employeeId, ct);
        }

        var ids = await _db.TerminalRegionMaps.AsNoTracking()
            .Where(x => x.RegionId == regionId)
            .Select(x => x.TerminalId)
            .ToListAsync(ct);

        var normalized = NormalizeTerminalIds(ids.ToArray());
        if (normalized.Count == 0)
        {
            TempData["Msg"] = "لا يوجد أجهزة ضمن هذه المنطقة.";
            return await Search(employeeId, ct);
        }

        try
        {
            var (ok, fail) = await RunThrottled(normalized, BulkMaxConcurrency, id =>
                _alpeta.AssignUserToTerminalAsync(id, employeeId, ct), ct);

            TempData["Msg"] = fail == 0
                ? $"✅ تم ربط كل أجهزة المنطقة بنجاح ({ok})."
                : $"⚠️ تم ربط {ok} جهاز من المنطقة، وفشل {fail} جهاز.";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "AssignRegion failed. employeeId={employeeId}, regionId={regionId}", employeeId, regionId);
            TempData["Msg"] = "❌ حدث خطأ أثناء ربط أجهزة المنطقة.";
        }

        return await Search(employeeId, ct);
    }

    // ✅ NEW: POST /employees/UnassignRegion  (فك ربط كل أجهزة منطقة واحدة)
    [HttpPost("UnassignRegion")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnassignRegion(int employeeId, int regionId, CancellationToken ct)
    {
        if (employeeId <= 0 || regionId <= 0)
        {
            TempData["Msg"] = "بيانات غير صحيحة.";
            return await Search(employeeId, ct);
        }

        var ids = await _db.TerminalRegionMaps.AsNoTracking()
            .Where(x => x.RegionId == regionId)
            .Select(x => x.TerminalId)
            .ToListAsync(ct);

        var normalized = NormalizeTerminalIds(ids.ToArray());
        if (normalized.Count == 0)
        {
            TempData["Msg"] = "لا يوجد أجهزة ضمن هذه المنطقة.";
            return await Search(employeeId, ct);
        }

        try
        {
            var (ok, fail) = await RunThrottled(normalized, BulkMaxConcurrency, id =>
                _alpeta.UnassignUserFromTerminalAsync(id, employeeId, ct), ct);

            TempData["Msg"] = fail == 0
                ? $"✅ تم إلغاء ربط كل أجهزة المنطقة بنجاح ({ok})."
                : $"⚠️ تم إلغاء ربط {ok} جهاز من المنطقة، وفشل {fail} جهاز.";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "UnassignRegion failed. employeeId={employeeId}, regionId={regionId}", employeeId, regionId);
            TempData["Msg"] = "❌ حدث خطأ أثناء إلغاء ربط أجهزة المنطقة.";
        }

        return await Search(employeeId, ct);
    }

    // ===== Helpers =====

    private static List<string> NormalizeTerminalIds(string[]? terminalIds)
    {
        if (terminalIds == null || terminalIds.Length == 0)
            return new List<string>();

        return terminalIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<(int ok, int fail)> RunThrottled(
        IReadOnlyCollection<string> ids,
        int maxConcurrency,
        Func<string, Task<bool>> work,
        CancellationToken ct)
    {
        using var sem = new SemaphoreSlim(maxConcurrency);

        var tasks = ids.Select(async id =>
        {
            await sem.WaitAsync(ct);
            try
            {
                return await work(id);
            }
            catch
            {
                return false;
            }
            finally
            {
                sem.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        var ok = results.Count(x => x);
        var fail = results.Length - ok;
        return (ok, fail);
    }
}
