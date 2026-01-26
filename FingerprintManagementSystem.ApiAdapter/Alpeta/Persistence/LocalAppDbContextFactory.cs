using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using FingerprintManagementSystem.ApiAdapter.Persistence;

namespace FingerprintManagementSystem.ApiAdapter.Persistence;

public class LocalAppDbContextFactory : IDesignTimeDbContextFactory<LocalAppDbContext>
{
    public LocalAppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LocalAppDbContext>()
            .UseSqlite("Data Source=../FingerprintManagementSystem.Web/App_Data/local.db")
            .Options;

        return new LocalAppDbContext(options);
    }
}