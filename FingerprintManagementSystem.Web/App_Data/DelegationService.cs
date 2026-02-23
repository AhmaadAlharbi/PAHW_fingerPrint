using FingerprintManagementSystem.ApiAdapter.Persistence;
using FingerprintManagementSystem.ApiAdapter.Persistence.Entities;
using FingerprintManagementSystem.Contracts;

namespace FingerprintManagementSystem.ApiAdapter.Implementations;

public class DelegationService : IDelegationService
{
    private readonly LocalAppDbContext _db;

    public DelegationService(LocalAppDbContext db)
    {
        _db = db;
    }

    public async Task<string> SaveDelegationAsync(
        int employeeId,
        List<string> terminalIds,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        if (terminalIds == null || terminalIds.Count == 0)
            return "Invalid";

        if (endDate < startDate)
            return "Invalid";

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

        return status;
    }
}
