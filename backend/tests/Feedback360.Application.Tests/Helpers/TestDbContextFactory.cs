using Feedback360.Application.Common;
using Feedback360.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Feedback360.Application.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    public static IApplicationDbContext CreateContext() => Create();
}
