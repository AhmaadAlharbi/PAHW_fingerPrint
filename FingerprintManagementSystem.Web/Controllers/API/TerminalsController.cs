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

    [HttpPost("auto-assign-regions")]
    public async Task<IActionResult> AutoAssignRegions(CancellationToken ct)
    {
        // 1) جلب المناطق والخرائط الحالية لتقليل استهلاك قاعدة البيانات
        var regionsList = await _db.Regions.AsNoTracking().ToListAsync(ct);
        var existingMaps = await _db.TerminalRegionMaps.ToDictionaryAsync(x => x.TerminalId, x => x, ct);

        var regionIds = regionsList
            .GroupBy(r => Normalize(r.Name))
            .ToDictionary(g => g.Key, g => g.First().Id);

        int? TryGetRegionId(string regionName)
            => regionIds.TryGetValue(Normalize(regionName), out var id) ? id : null;

        static bool ContainsAny(string normalizedText, params string[] tokens)
            => tokens.Any(t => normalizedText.Contains(Normalize(t), StringComparison.OrdinalIgnoreCase));

        // 2) منطق التوزيع المحدث (يشمل اختلاف الـ spelling + السالمي)
        int? GetRegionIdByTerminal(int terminalId, string terminalName)
        {
            var other = TryGetRegionId("مواقع أخرى");
            if (other is null) return null;

            // المبنى الرئيسي (IDs 3..26)
            if (terminalId >= 3 && terminalId <= 26)
                return TryGetRegionId("المبنى الرئيسي") ?? other;

            var n = Normalize(terminalName);

            // المطلاع
            if (ContainsAny(n, "mutlaa", "mutla", "المطلاع", "مطلاع"))
                return TryGetRegionId("المطلاع") ?? other;

            // برج التحرير
            if (ContainsAny(n, "liberation", "tower", "تحرير", "برج التحرير"))
                return TryGetRegionId("برج التحرير") ?? other;

            // صباح السالم (يغطي: SABAH ELSALEM / SABAH SALIM / صباح السالم)
            if (ContainsAny(n,
                    "sabah al salem", "sabah elsalem", "sabah el salem", "sabah salem", "sabah salim",
                    "صباح السالم", "صباح سالم"))
                return TryGetRegionId("صباح السالم") ?? other;

            // سعد العبدالله
            if (ContainsAny(n, "saad", "سعد"))
                return TryGetRegionId("سعد العبدالله") ?? other;

            // جابر الأحمد
            if (ContainsAny(n, "jaber", "جابر"))
                return TryGetRegionId("جابر الأحمد") ?? other;

            // الصليبية (يغطي: Sulaibia / Sulaibiya)
            if (ContainsAny(n, "sulaibia", "sulaibiya", "sulaibi", "الصليبية", "صليبية"))
                return TryGetRegionId("الصليبية") ?? other;

            // مبارك الكبير (يغطي: Mubarak / Mubark Alkabir)
            if (ContainsAny(n, "mubarak", "mubark", "alkabir", "al kabir", "مبارك الكبير"))
                return TryGetRegionId("مبارك الكبير") ?? other;

            // النهضة
            if (ContainsAny(n, "nahda", "النهضة", "نهضة"))
                return TryGetRegionId("النهضة") ?? other;

            // غرب الجليب
            if (ContainsAny(n, "west jleeb", "west jleib", "jleeb", "غرب الجليب"))
                return TryGetRegionId("غرب الجليب") ?? other;

            // السالمي (جديد) — يغطي: Salmi / السالمي
            if (ContainsAny(n, "salmi", "السالمي", "سالمي"))
                return TryGetRegionId("السالمي") ?? other;

            // الجهراء (حكومة مول / تيماء) — يغطي: Jahra/Jahrah
            if (ContainsAny(n, "jahra", "jahrah", "جهراء", "الجهراء"))
            {
                if (ContainsAny(n, "taima", "tayma", "تيماء"))
                    return TryGetRegionId("الجهراء - تيماء") ?? other;

                return TryGetRegionId("الجهراء - حكومة مول") ?? other;
            }

            // القرين - حكومة مول
            if (ContainsAny(n, "qurain", "قرين", "القرين"))
                return TryGetRegionId("القرين - حكومة مول") ?? other;

            return other;
        }

        // 3) جلب الأجهزة من Alpeta API
        var devices = await _alpeta.GetAllDevicesAsync(ct);

        int inserted = 0, updated = 0, skippedInvalidId = 0;

        foreach (var d in devices)
        {
            if (!int.TryParse(d.DeviceId, out var tid))
            {
                skippedInvalidId++;
                continue;
            }

            var targetRegionId = GetRegionIdByTerminal(tid, d.DeviceName);
            if (targetRegionId == null) continue;

            if (existingMaps.TryGetValue(d.DeviceId, out var existingMap))
            {
                if (existingMap.RegionId != targetRegionId.Value)
                {
                    existingMap.RegionId = targetRegionId.Value;
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
            message = "تم تحديث توزيع الأجهزة بنجاح",
            totalDevices = devices.Count,
            newlyAssigned = inserted,
            updatedRegions = updated,
            invalidDeviceIds = skippedInvalidId
        });
    }

    // Normalize أقوى: يشيل الرموز ويخلي النص قابل للمطابقة حتى لو الاسم فيه - _ () أو مسافات زايدة
    private static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;

        s = s.Trim().ToLowerInvariant();

        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            // نخلي الحروف والأرقام (عربي/انجليزي) ونحوّل الباقي لمسافة
            if (char.IsLetterOrDigit(ch) || ch == ' ' || (ch >= 0x0600 && ch <= 0x06FF))
                sb.Append(ch);
            else
                sb.Append(' ');
        }

        // Collapse spaces
        var normalized = string.Join(' ', sb
            .ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return normalized;
    }
}
