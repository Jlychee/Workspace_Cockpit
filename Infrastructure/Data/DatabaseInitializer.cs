using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class DatabaseInitializer(IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(DbPathProvider.GetDatabaseDirectory());

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.Database.MigrateAsync();
    }
}