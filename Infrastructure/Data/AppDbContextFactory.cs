using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        Directory.CreateDirectory(DbPathProvider.GetDatabaseDirectory());

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(DbPathProvider.GetConnectionString())
            .Options;

        return new AppDbContext(options);
    }
}
