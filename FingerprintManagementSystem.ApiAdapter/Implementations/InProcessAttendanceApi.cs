using FingerprintManagementSystem.Contracts;
using FingerprintManagementSystem.Contracts.DTOs;

namespace FingerprintManagementSystem.ApiAdapter.Implementations;

public class InProcessAttendanceApi : IAttendanceApi
{
    public Task<EmployeeDto?> GetEmployeeByIdAsync(int employeeId, CancellationToken ct = default)
    {
        if (employeeId <= 0) return Task.FromResult<EmployeeDto?>(null);

        return Task.FromResult<EmployeeDto?>(new EmployeeDto
        {
            EmployeeId = employeeId,
            FullNameAr = "موظف تجريبي",
            Department = "IT",
            JobTitle = "Developer"
        });
    }

    public async Task<EmployeeDevicesDto?> GetEmployeeWithDevicesAsync(int employeeId, CancellationToken ct = default)
    {
        var emp = await GetEmployeeByIdAsync(employeeId, ct);
        if (emp == null) return null;

        // Dummy devices
        var all = new List<DeviceDto>
        {
            new() { DeviceId="D1", DeviceName="Main Gate", Location="HQ" },
            new() { DeviceId="D2", DeviceName="IT Floor", Location="Building B" },
        };

        return new EmployeeDevicesDto
        {
            Employee = emp,
            AllDevices = all,
            AssignedDevices = all.Take(1).ToList()
        };
    }
}
