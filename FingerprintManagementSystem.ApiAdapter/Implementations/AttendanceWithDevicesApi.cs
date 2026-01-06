using FingerprintManagementSystem.ApiAdapter.Alpeta;
using FingerprintManagementSystem.ApiAdapter.Soap;
using FingerprintManagementSystem.Contracts;
using FingerprintManagementSystem.Contracts.DTOs;

namespace FingerprintManagementSystem.ApiAdapter.Implementations;

public class AttendanceWithDevicesApi : IAttendanceApi
{
    private readonly EmployeeSoapClient _soap;
    private readonly AlpetaClient _alpeta;

    public AttendanceWithDevicesApi(EmployeeSoapClient soap, AlpetaClient alpeta)
    {
        _soap = soap;
        _alpeta = alpeta;
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int employeeId, CancellationToken ct = default)
    {
        var model = await GetEmployeeWithDevicesAsync(employeeId, ct);
        return model?.Employee;
    }

    public async Task<EmployeeDevicesDto?> GetEmployeeWithDevicesAsync(int employeeId, CancellationToken ct = default)
    {
        if (employeeId <= 0) return null;

        // 1) Ping Alpeta (اختياري)
        var ok = await _alpeta.PingAsync();
        if (!ok) throw new Exception("Alpeta API not reachable");

        // 2) Employee from SOAP
        var raw = await _soap.GetEmployeeByIdRawAsync(employeeId, ct);
        var (name, dept, title) = _soap.ParseEmployeeSummary(raw);

        if (string.IsNullOrWhiteSpace(name))
            return null;

        var emp = new EmployeeDto
        {
            EmployeeId = employeeId,
            FullNameAr = name,
            Department = dept,
            JobTitle = title
        };

        // 3) Devices from Alpeta (لا نكسر الصفحة لو Alpeta رجع خطأ)
        List<DeviceDto> allDevices;
        List<DeviceDto> assignedDevices;

        try
        {
            allDevices = await _alpeta.GetAllDevicesAsync(ct);
        }
        catch
        {
            allDevices = new List<DeviceDto>();
        }

        try
        {
            assignedDevices = await _alpeta.GetEmployeeDevicesAsync(employeeId, ct);
        }
        catch
        {
            assignedDevices = new List<DeviceDto>();
        }

        return new EmployeeDevicesDto
        {
            Employee = emp,
            AllDevices = allDevices,
            AssignedDevices = assignedDevices
        };
    }
}
