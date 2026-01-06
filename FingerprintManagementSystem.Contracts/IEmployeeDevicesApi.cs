using FingerprintManagementSystem.Contracts.DTOs;

namespace FingerprintManagementSystem.Contracts;

public interface IEmployeeDevicesApi
{
    Task<EmployeeDevicesDto?> GetEmployeeWithDevicesAsync(int employeeId, CancellationToken ct = default);
}
