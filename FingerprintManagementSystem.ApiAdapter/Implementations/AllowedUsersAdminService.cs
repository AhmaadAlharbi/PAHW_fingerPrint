using FingerprintManagementSystem.Contracts;
using FingerprintManagementSystem.ApiAdapter.Persistence;
using FingerprintManagementSystem.ApiAdapter.Persistence.Entities;
using FingerprintManagementSystem.ApiAdapter.Soap;
using Microsoft.EntityFrameworkCore;

namespace FingerprintManagementSystem.ApiAdapter;

public class AllowedUsersAdminService : IAllowedUsersAdmin
{
    private readonly LocalAppDbContext _db;
    private readonly EmployeeSoapClient _soap;

    public AllowedUsersAdminService(LocalAppDbContext db, EmployeeSoapClient soap)
    {
        _db = db;
        _soap = soap;
    }

    public async Task<AllowedUserDto?> FetchFromSoapAsync(int employeeId, CancellationToken ct)
    {
        var rawXml = await _soap.GetEmployeeByIdRawAsync(employeeId, ct);

        // Name + Department + JobTitle (JobTitle ما نحتاجه هنا)
        var (name, dept, _) = _soap.ParseEmployeeSummary(rawXml);

        // نجيب كل الحقول عشان نطلع الإيميل لو موجود
        var fields = _soap.DumpEmployeeDetailFields(rawXml);

        // حاول نلقط الإيميل بأكثر من اسم محتمل
        string email = "";
        var emailKeys = new[] { "email", "emailAddress", "mail", "workEmail", "employeeEmail" };

        foreach (var k in emailKeys)
        {
            if (fields.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
            {
                email = v.Trim();
                break;
            }
        }

        // إذا ما رجع اسم/قسم، غالبًا الموظف غير موجود أو SOAP رجع Fault
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(dept) && string.IsNullOrWhiteSpace(email))
            return null;

        return new AllowedUserDto(
            employeeId,
            name ?? "",
            email,
            dept ?? ""
        );
    }


    public async Task<bool> AddAsync(AllowedUserDto dto, DateTime? validUntil, bool isAdmin, CancellationToken ct)
    {
        var exists = await _db.AllowedUsers.AnyAsync(x => x.EmployeeId == dto.EmployeeId, ct);
        if (exists) return false;

        _db.AllowedUsers.Add(new AllowedUser
        {
            EmployeeId = dto.EmployeeId,
            FullName = dto.FullName,
            Email = dto.Email,
            Department = dto.Department,
            ValidUntil = validUntil,
            IsAdmin = isAdmin,
            IsActive = true
        });

        await _db.SaveChangesAsync(ct);
        return true;
    }
    public async Task<List<AllowedUserListItemDto>> ListAsync(CancellationToken ct)
    {
        var rows = await _db.AllowedUsers
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.EmployeeId)
            .Select(x => new AllowedUserListItemDto(
                x.EmployeeId,
                x.FullName,
                x.Email,
                x.Department,
                x.IsActive,
                x.IsAdmin,
                x.ValidUntil
            ))
            .ToListAsync(ct);

        return rows;
    }

    public async Task<bool> SetActiveAsync(int employeeId, bool isActive, CancellationToken ct)
    {
        var user = await _db.AllowedUsers
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId, ct);

        if (user == null)
            return false;

        // ❗ لو بنعطّل Admin، تأكد مو آخر واحد
        if (!isActive && user.IsAdmin)
        {
            var adminCount = await _db.AllowedUsers
                .CountAsync(x => x.IsAdmin && x.IsActive, ct);

            if (adminCount <= 1)
                return false; // لا تعطّل آخر Admin
        }

        // ✅ الآن نغيّر
        user.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
        return true;
    }


    public async Task<bool> DeleteAsync(int employeeId, CancellationToken ct)
    {
        var user = await _db.AllowedUsers
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId, ct);

        if (user == null)
            return false;

        // لا تحذف آخر Admin
        if (user.IsAdmin)
        {
            var adminCount = await _db.AllowedUsers
                .CountAsync(x => x.IsAdmin && x.IsActive, ct);

            if (adminCount <= 1)
                return false;
        }

        _db.AllowedUsers.Remove(user);
        await _db.SaveChangesAsync(ct);
        return true;
    }



}