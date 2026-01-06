using FingerprintManagementSystem.ApiAdapter.Alpeta;
using FingerprintManagementSystem.ApiAdapter.Persistence;
using FingerprintManagementSystem.Contracts;
using FingerprintManagementSystem.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FingerprintManagementSystem.Web.Controllers;

public class EmployeesController : Controller
{
    private readonly IAttendanceApi _api;
    private readonly AlpetaClient _alpeta;
    private readonly LocalAppDbContext _db;
    private readonly ILogger<EmployeesController> _log;

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

    [HttpGet]
    public IActionResult Search() => View(new EmployeeDevicesViewModel());

    [HttpPost]
    public async Task<IActionResult> Search(int employeeId, CancellationToken ct)
    {
        var vm = await BuildEmployeeDevicesVm(employeeId, ct);
        return View(vm);
    }

    // زر ربط
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int employeeId, string terminalId, CancellationToken ct)
    {
        if (employeeId <= 0 || string.IsNullOrWhiteSpace(terminalId))
            return RedirectToAction(nameof(Search));

        try
        {
            var ok = await _alpeta.AssignUserToTerminalAsync(terminalId, employeeId, ct);
            TempData["Msg"] = ok ? "تم ربط الموظف بالجهاز ✅" : "لم يتم الربط (تحقق من Alpeta) ❌";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Assign failed employeeId={employeeId}, terminalId={terminalId}", employeeId, terminalId);
            TempData["Msg"] = "صار خطأ أثناء الربط. راجع Logs.";
        }

        // رجّع نفس نتيجة البحث بعد الربط
        var vm = await BuildEmployeeDevicesVm(employeeId, ct);
        return View("Search", vm);
    }

    // زر إلغاء ربط
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unassign(int employeeId, string terminalId, CancellationToken ct)
    {
        if (employeeId <= 0 || string.IsNullOrWhiteSpace(terminalId))
            return RedirectToAction(nameof(Search));

        try
        {
            var ok = await _alpeta.UnassignUserFromTerminalAsync(terminalId, employeeId, ct);
            TempData["Msg"] = ok ? "تم إلغاء ربط الموظف بالجهاز ✅" : "لم يتم الإلغاء (تحقق من Alpeta) ❌";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unassign failed employeeId={employeeId}, terminalId={terminalId}", employeeId, terminalId);
            TempData["Msg"] = "صار خطأ أثناء إلغاء الربط. راجع Logs.";
        }

        var vm = await BuildEmployeeDevicesVm(employeeId, ct);
        return View("Search", vm);
    }

    // ====== Helpers ======

    private async Task<EmployeeDevicesViewModel> BuildEmployeeDevicesVm(int employeeId, CancellationToken ct)
    {
        var vm = new EmployeeDevicesViewModel();

        if (employeeId <= 0)
            return vm;

        try
        {
            var result = await _api.GetEmployeeWithDevicesAsync(employeeId, ct);
            if (result is null)
            {
                vm.ErrorMessage = "الموظف غير موجود";
                return vm;
            }

            vm.Employee = result.Employee;

            // TerminalId -> Region
            var regionPairs = await (
                from m in _db.TerminalRegionMaps.AsNoTracking()
                join r in _db.Regions.AsNoTracking() on m.RegionId equals r.Id
                select new
                {
                    TerminalId = m.TerminalId.ToString(),
                    RegionId = (int?)m.RegionId,
                    RegionName = r.Name
                }
            ).ToListAsync(ct);

            var regionByTerminal = regionPairs
                .GroupBy(x => x.TerminalId)
                .ToDictionary(g => g.Key, g => (g.First().RegionId, g.First().RegionName));

            // IDs للأجهزة المرتبطة بالموظف (نفلتر null/empty)
            var assignedIds = new HashSet<string>(
                (result.AssignedDevices ?? new List<FingerprintManagementSystem.Contracts.DTOs.DeviceDto>())
                    .Select(d => d.DeviceId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => id!.Trim()),
                StringComparer.OrdinalIgnoreCase
            );

            vm.Devices = (result.AllDevices ?? new List<FingerprintManagementSystem.Contracts.DTOs.DeviceDto>())
               .Select(d =>
               {
                   var deviceId = d.DeviceId?.Trim() ?? "";

                   int? regionId = null;
                   string? regionName = null;

                   if (!string.IsNullOrWhiteSpace(deviceId) && regionByTerminal.TryGetValue(deviceId, out var reg))
                   {
                       regionId = reg.RegionId;
                       regionName = reg.RegionName;
                   }

                   return new DeviceRowVm
                   {
                       DeviceId = deviceId,
                       DeviceName = d.DeviceName,
                       Location = d.Location,
                       IsAssigned = !string.IsNullOrWhiteSpace(deviceId) && assignedIds.Contains(deviceId),
                       RegionId = regionId,
                       RegionName = regionName
                   };
               })
               .OrderByDescending(x => x.IsAssigned)
               .ThenBy(x => x.RegionName)
               .ThenBy(x => x.DeviceName)
               .ToList();


            return vm;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Employee search failed for employeeId={employeeId}", employeeId);
            vm.ErrorMessage = "صار خطأ أثناء البحث. راجع Logs.";
            return vm;
        }
    }
}
