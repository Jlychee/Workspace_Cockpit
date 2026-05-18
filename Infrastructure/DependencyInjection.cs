using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkspaceCockpitInfrastructure(this IServiceCollection services)
    {
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite(DbPathProvider.GetConnectionString()));

        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<WorkspaceRepository>();

        return services;
    }
}
