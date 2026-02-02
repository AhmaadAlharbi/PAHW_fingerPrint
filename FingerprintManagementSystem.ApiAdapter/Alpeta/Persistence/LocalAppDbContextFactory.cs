using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using FingerprintManagementSystem.ApiAdapter.Persistence;

namespace FingerprintManagementSystem.ApiAdapter.Persistence;

public class LocalAppDbContextFactory : IDesignTimeDbContextFactory<LocalAppDbContext>
{
    public LocalAppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LocalAppDbContext>()
            .UseSqlServer("Server=localhost;Database=FingerprintLocal;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new LocalAppDbContext(options);
    }
}