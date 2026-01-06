using FingerprintManagementSystem.Contracts.DTOs;

namespace FingerprintManagementSystem.Web.ViewModels;

public class EmployeeDevicesViewModel
{
    public EmployeeDto Employee { get; set; } = new();
    public List<DeviceRowVm> Devices { get; set; } = new();
    public string ErrorMessage { get; internal set; }
    public int TotalDevices { get; internal set; }
    public int AssignedCount { get; internal set; }
}
