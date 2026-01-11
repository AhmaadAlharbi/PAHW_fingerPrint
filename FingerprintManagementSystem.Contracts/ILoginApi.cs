using FingerprintManagementSystem.Contracts.DTOs;

namespace FingerprintManagementSystem.Contracts;

public interface ILoginApi
{
    Task<LoginResponseDto> LoginAsync(string empId, string password, CancellationToken ct = default);
}
