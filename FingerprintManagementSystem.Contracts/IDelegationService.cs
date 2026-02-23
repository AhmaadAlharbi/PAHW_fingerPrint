namespace FingerprintManagementSystem.Contracts;

public interface IDelegationService
{
    Task<string> SaveDelegationAsync(
        int employeeId,
        List<string> terminalIds,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default);
}
