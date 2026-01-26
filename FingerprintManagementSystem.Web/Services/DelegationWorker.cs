using FingerprintManagementSystem.ApiAdapter.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FingerprintManagementSystem.Contracts;

namespace FingerprintManagementSystem.Web.Services;

public class DelegationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DelegationWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<LocalAppDbContext>();
                var api = scope.ServiceProvider.GetRequiredService<IEmployeeDevicesApi>();

                var now = DateTime.Now;

                // 1) START: Scheduled -> Active (Assign)
                var toStart = await db.Delegations
                    .Where(x => x.Status == "Scheduled" && x.StartDate <= now)
                    .ToListAsync(stoppingToken);

                foreach (var del in toStart)
                {
                    var terminals = await db.DelegationTerminals
                        .Where(t => t.DelegationId == del.Id)
                        .Select(t => t.TerminalId)
                        .ToListAsync(stoppingToken);

                    foreach (var terminalId in terminals)
                        await api.AssignOneAsync(del.EmployeeId, terminalId, stoppingToken);

                    del.Status = "Active";
                    del.ActivatedAt = now;
                }

                // 2) EXPIRE: Active -> Expired (Unassign)
                var toExpire = await db.Delegations
                    .Where(x => x.Status == "Active" && x.EndDate <= now)
                    .ToListAsync(stoppingToken);

                foreach (var del in toExpire)
                {
                    var terminals = await db.DelegationTerminals
                        .Where(t => t.DelegationId == del.Id)
                        .Select(t => t.TerminalId)
                        .ToListAsync(stoppingToken);

                    foreach (var terminalId in terminals)
                        await api.UnassignOneAsync(del.EmployeeId, terminalId, stoppingToken);

                    del.Status = "Expired";
                    del.ExpiredAt = now;
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch
            {
                // لا توقف التطبيق لو صار خطأ مؤقت
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
