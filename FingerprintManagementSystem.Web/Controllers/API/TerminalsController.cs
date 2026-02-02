using FingerprintManagementSystem.ApiAdapter.Alpeta;
using FingerprintManagementSystem.ApiAdapter.Persistence;
using FingerprintManagementSystem.ApiAdapter.Persistence.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
namespace FingerprintManagementSystem.Web.Controllers.Api;

[ApiController]
[Route("api/terminals")]
public class TerminalsController : ControllerBase
{
    private readonly LocalAppDbContext _db;
    private readonly AlpetaClient _alpeta;

    public TerminalsController(LocalAppDbContext db, AlpetaClient alpeta)
    {
        _db = db;
        _alpeta = alpeta;
    }

    // 🔹 جلب المناطق (للبحث / dropdown)
    [HttpGet("regions")]
    public async Task<IActionResult> GetRegions(CancellationToken ct)
    {
        var regions = await _db.Regions
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, r.Name })
            .ToListAsync(ct);

        return Ok(new { total = regions.Count, regions });
    }

    // 🔹 جلب توزيع الأجهزة الحالي
    [HttpGet]
    public async Task<IActionResult> GetMaps(CancellationToken ct)
    {
        var maps = await (
            from m in _db.TerminalRegionMaps.AsNoTracking()
            join r in _db.Regions.AsNoTracking() on m.RegionId equals r.Id
            orderby m.TerminalId
            select new
            {
                terminalId = m.TerminalId,
                regionId = m.RegionId,
                regionName = r.Name
            }
        ).ToListAsync(ct);

        return Ok(new { total = maps.Count, maps });
    }

    // 🔹 توزيع / إعادة توزيع الأجهزة (SAFE – يشتغل أكثر من مرة)
    [HttpPost("auto-assign-regions")]
    public async Task<IActionResult> AutoAssignRegions(CancellationToken ct)
    {
        // 🔒 Admin only (يعتمد على اللي تسويه عند تسجيل الدخول)
        if (HttpContext.Session.GetString("IsAdmin") != "1")
            return Forbid(); // 403

        var regions = await _db.Regions.AsNoTracking().ToListAsync(ct);

        // خله Tracking لأننا بنعدل على entities
        var existingMaps = await _db.TerminalRegionMaps
            .ToDictionaryAsync(x => x.TerminalId, x => x, ct);

        var regionIds = regions
            .GroupBy(r => Normalize(r.Name))
            .ToDictionary(g => g.Key, g => g.First().Id);

        int? TryGetRegionId(string name)
            => regionIds.TryGetValue(Normalize(name), out var id) ? id : null;

        var devices = await _alpeta.GetAllDevicesAsync(ct);

        int inserted = 0, updated = 0, skippedInvalidId = 0;

        foreach (var d in devices)
        {
            if (!int.TryParse(d.DeviceId, out _))
            {
                skippedInvalidId++;
                continue;
            }

            var targetRegionId = TryGetRegionId(d.DeviceName)
                                 ?? TryGetRegionId("مواقع أخرى");

            if (targetRegionId == null)
                continue;

            if (existingMaps.TryGetValue(d.DeviceId, out var map))
            {
                if (map.RegionId != targetRegionId.Value)
                {
                    map.RegionId = targetRegionId.Value;
                    updated++;
                }
            }
            else
            {
                _db.TerminalRegionMaps.Add(new TerminalRegionMap
                {
                    TerminalId = d.DeviceId,
                    RegionId = targetRegionId.Value
                });
                inserted++;
            }
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            message = "تم تحديث توزيع الأجهزة",
            totalDevices = devices.Count,
            inserted,
            updated,
            invalidDeviceIds = skippedInvalidId
        });
    }

    // 🔹 Normalize للأسماء
    private static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;

        s = s.Trim().ToLowerInvariant();
        var sb = new StringBuilder();

        foreach (var ch in s)
        {
            if (char.IsLetterOrDigit(ch) || ch == ' ' || (ch >= 0x0600 && ch <= 0x06FF))
                sb.Append(ch);
            else
                sb.Append(' ');
        }

        return string.Join(' ',
            sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
