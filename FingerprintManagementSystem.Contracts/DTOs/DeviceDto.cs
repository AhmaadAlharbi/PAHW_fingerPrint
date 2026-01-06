using System;
using System.Collections.Generic;
using System.Text;

namespace FingerprintManagementSystem.Contracts.DTOs
{
    public class DeviceDto
    {
        public string DeviceId { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public string Location { get; set; } = "";
    }
}
