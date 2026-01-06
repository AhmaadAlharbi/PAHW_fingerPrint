namespace FingerprintManagementSystem.Web.ViewModels;

public class EmployeeDevicesViewModel
{
    public FingerprintManagementSystem.Contracts.DTOs.EmployeeDto? Employee { get; set; }

    public List<DeviceRowVm> Devices { get; set; } = new();

    // ✅ الجديد: تجميع حسب المناطق للعرض بشكل Accordion
    public List<RegionGroupVm> RegionGroups { get; set; } = new();

    public string? ErrorMessage { get; set; }
}
