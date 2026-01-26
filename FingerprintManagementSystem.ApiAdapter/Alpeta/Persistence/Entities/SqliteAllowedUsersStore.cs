using FingerprintManagementSystem.Contracts;
using FingerprintManagementSystem.ApiAdapter.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FingerprintManagementSystem.ApiAdapter;

public class SqliteAllowedUsersStore : IAllowedUsersStore
{
    private readonly LocalAppDbContext _db;

    public SqliteAllowedUsersStore(LocalAppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsAllowedAsync(int employeeId, CancellationToken ct)
    {
        var user = await _db.AllowedUsers
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.IsActive, ct);

        if (user == null) return false;

        if (user.ValidUntil.HasValue && user.ValidUntil.Value.Date < DateTime.Today)
            return false;

        return true;
    }
}