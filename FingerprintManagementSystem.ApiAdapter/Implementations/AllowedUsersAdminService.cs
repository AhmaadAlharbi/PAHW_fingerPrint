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
}