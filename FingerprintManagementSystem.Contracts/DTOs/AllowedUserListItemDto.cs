namespace FingerprintManagementSystem.Contracts;

public record AllowedUserListItemDto(
    int EmployeeId,
    string FullName,
    string Email,
    string Department,
    bool IsActive,
    bool IsAdmin,
    DateTime? ValidUntil
);