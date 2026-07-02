using Feedback360.Application.Common;
using Feedback360.Application.DTOs;
using Feedback360.Domain.Entities;
using Feedback360.Domain.Enums;
using Feedback360.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Feedback360.Infrastructure;

public static class SeedData
{
    public static readonly Guid SkillsCategoryId = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000001");
    public static readonly Guid PerformanceCategoryId = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000002");
    public static readonly Guid LeadershipCategoryId = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000003");
    public static readonly Guid TeamworkCategoryId = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000004");
    public static readonly Guid CommunicationCategoryId = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000005");

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var env = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        if (env.EnvironmentName == "Testing")
            await db.Database.EnsureCreatedAsync();
        else
            await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync())
        {
            await SeedUsersAsync(db, hasher);
        }

        await SeedCategoriesAsync(db);
    }

    private static async Task SeedUsersAsync(AppDbContext db, IPasswordHasher hasher)
    {
        var admin = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "admin@feedback360.local",
            Name = "System Admin",
            Role = UserRole.Admin,
            IsActive = true,
            AuthProvider = "local",
            PasswordHash = hasher.Hash("Admin123!")
        };

        var manager = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Email = "manager@feedback360.local",
            Name = "Jane Manager",
            Role = UserRole.Manager,
            IsActive = true,
            AuthProvider = "local",
            PasswordHash = hasher.Hash("Manager123!")
        };

        var employee1 = new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Email = "alice@feedback360.local",
            Name = "Alice Employee",
            Role = UserRole.Employee,
            ManagerId = manager.Id,
            IsActive = true,
            AuthProvider = "local",
            PasswordHash = hasher.Hash("Employee123!")
        };

        var employee2 = new User
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Email = "bob@feedback360.local",
            Name = "Bob Employee",
            Role = UserRole.Employee,
            ManagerId = manager.Id,
            IsActive = true,
            AuthProvider = "local",
            PasswordHash = hasher.Hash("Employee123!")
        };

        db.Users.AddRange(admin, manager, employee1, employee2);
        await db.SaveChangesAsync();
    }

    private static async Task SeedCategoriesAsync(AppDbContext db)
    {
        if (await db.QuestionCategories.AnyAsync()) return;

        var creatorId = await db.Users
            .Where(u => u.Role == UserRole.Manager)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
        if (creatorId == Guid.Empty)
        {
            creatorId = await db.Users.Select(u => u.Id).FirstAsync();
        }

        var defaults = new[]
        {
            (SkillsCategoryId, "Skills"),
            (PerformanceCategoryId, "Performance"),
            (LeadershipCategoryId, "Leadership"),
            (TeamworkCategoryId, "Teamwork"),
            (CommunicationCategoryId, "Communication"),
        };

        foreach (var (id, name) in defaults)
        {
            db.QuestionCategories.Add(new QuestionCategory
            {
                Id = id,
                Name = name,
                CreatedById = creatorId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }
}
