using FingerprintManagementSystem.Contracts;
using FingerprintManagementSystem.Contracts.DTOs;
using FingerprintManagementSystem.ApiAdapter.Soap;

namespace FingerprintManagementSystem.ApiAdapter.Implementations;

public class SoapAttendanceApi : IAttendanceApi
{
    private readonly EmployeeSoapClient _soap;

    public SoapAttendanceApi(EmployeeSoapClient soap)
    {
        _soap = soap;
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int employeeId, CancellationToken ct = default)
    {
        if (employeeId <= 0) return null;

        var raw = await _soap.GetEmployeeByIdRawAsync(employeeId, ct);
        var (name, dept, title) = _soap.ParseEmployeeSummary(raw);

        if (string.IsNullOrWhiteSpace(name)) return null;

        return new EmployeeDto
        {
            EmployeeId = employeeId,
            FullNameAr = name,
            Department = dept,
            JobTitle = title
        };
    }

    public async Task<EmployeeDevicesDto?> GetEmployeeWithDevicesAsync(int employeeId, CancellationToken ct = default)
    {
        var emp = await GetEmployeeByIdAsync(employeeId, ct);
        if (emp == null) return null;

        return new EmployeeDevicesDto
        {
            Employee = emp,
            AllDevices = new List<DeviceDto>(),
            AssignedDevices = new List<DeviceDto>()
        };
    }
}
