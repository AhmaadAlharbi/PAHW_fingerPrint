namespace FingerprintManagementSystem.Contracts;

public record AllowedUserDto(
    int EmployeeId,
    string FullName,
    string Email,
    string Department
);