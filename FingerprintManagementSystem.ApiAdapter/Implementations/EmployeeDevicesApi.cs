using FingerprintManagementSystem.ApiAdapter.Alpeta;
using FingerprintManagementSystem.ApiAdapter.Soap;
using FingerprintManagementSystem.Contracts;
using FingerprintManagementSystem.Contracts.DTOs;

namespace FingerprintManagementSystem.ApiAdapter.Implementations;

/// <summary>
/// Simple API that returns:
/// - Employee summary from SOAP
/// - All devices from Alpeta
/// - Devices assigned to employee from Alpeta
///
/// NOTE: Region/assignment flags are computed in the Web layer (ViewModel)
/// to avoid introducing new DTO types.
/// </summary>
public class EmployeeDevicesApi : IEmployeeDevicesApi
{
    private readonly EmployeeSoapClient _soap;
    private readonly AlpetaClient _alpeta;

    public EmployeeDevicesApi(EmployeeSoapClient soap, AlpetaClient alpeta)
    {
        _soap = soap;
        _alpeta = alpeta;
    }

    public async Task<EmployeeDevicesDto?> GetEmployeeWithDevicesAsync(int employeeId, CancellationToken ct = default)
    {
        if (employeeId <= 0)
            return null;

        // 1) Employee from SOAP
        var raw = await _soap.GetEmployeeByIdRawAsync(employeeId, ct);
        var (name, dept, title) = _soap.ParseEmployeeSummary(raw);
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var employee = new EmployeeDto
        {
            EmployeeId = employeeId,
            FullNameAr = name,
            Department = dept,
            JobTitle = title
        };

        // 2) Devices from Alpeta
        var allDevices = await _alpeta.GetAllDevicesAsync(ct);
        var assignedDevices = await _alpeta.GetEmployeeDevicesAsync(employeeId, ct);

        return new EmployeeDevicesDto
        {
            Employee = employee,
            AllDevices = allDevices,
            AssignedDevices = assignedDevices
        };
    }
}
