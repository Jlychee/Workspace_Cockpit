using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class DatabaseInitializer
{
    private readonly IDbContextFactory<AppDbContext> dbContextFactory;

    public DatabaseInitializer(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(DbPathProvider.GetDatabaseDirectory());

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.Database.MigrateAsync();
    }
}
