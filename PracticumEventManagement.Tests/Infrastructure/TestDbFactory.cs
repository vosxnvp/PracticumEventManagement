using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PracticumEventManagement.DataAccess;

namespace PracticumEventManagement.Tests.Infrastructure;

internal static class TestDbFactory
{
    public static ServiceProvider CreateProvider()
    {
        var dbName = Guid.NewGuid().ToString("N");

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        return services.BuildServiceProvider();
    }
}