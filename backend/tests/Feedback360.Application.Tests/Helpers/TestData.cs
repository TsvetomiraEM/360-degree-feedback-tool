using Feedback360.Application.Common;
using Feedback360.Domain.Entities;
using Feedback360.Domain.Enums;
using Feedback360.Infrastructure.Auth;
using Feedback360.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Feedback360.Application.Tests.Helpers;

public static class TestIds
{
    public static readonly Guid AdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ManagerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid Employee1Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid Employee2Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid OtherManagerId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid SkillsCategoryId = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000001");
    public static readonly Guid TemplateId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid SurveyId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid AssignmentId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    public static readonly Guid RatingQuestionId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    public static readonly Guid OpenTextQuestionId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
}

public static class TestData
{
    public static async Task SeedUsersAsync(AppDbContext db, IPasswordHasher? hasher = null)
    {
        hasher ??= new PasswordHasher();
        if (await db.Users.AnyAsync()) return;

        db.Users.AddRange(
            new User
            {
                Id = TestIds.AdminId,
                Email = "admin@feedback360.local",
                Name = "System Admin",
                Role = UserRole.Admin,
                IsActive = true,
                AuthProvider = "local",
                PasswordHash = hasher.Hash("Admin123!")
            },
            new User
            {
                Id = TestIds.ManagerId,
                Email = "manager@feedback360.local",
                Name = "Jane Manager",
                Role = UserRole.Manager,
                IsActive = true,
                AuthProvider = "local",
                PasswordHash = hasher.Hash("Manager123!")
            },
            new User
            {
                Id = TestIds.OtherManagerId,
                Email = "other@feedback360.local",
                Name = "Other Manager",
                Role = UserRole.Manager,
                IsActive = true,
                AuthProvider = "local",
                PasswordHash = hasher.Hash("Manager123!")
            },
            new User
            {
                Id = TestIds.Employee1Id,
                Email = "alice@feedback360.local",
                Name = "Alice Employee",
                Role = UserRole.Employee,
                ManagerId = TestIds.ManagerId,
                IsActive = true,
                AuthProvider = "local",
                PasswordHash = hasher.Hash("Employee123!")
            },
            new User
            {
                Id = TestIds.Employee2Id,
                Email = "bob@feedback360.local",
                Name = "Bob Employee",
                Role = UserRole.Employee,
                ManagerId = TestIds.ManagerId,
                IsActive = true,
                AuthProvider = "local",
                PasswordHash = hasher.Hash("Employee123!")
            });
        await db.SaveChangesAsync();
    }

    public static async Task SeedCategoryAsync(AppDbContext db)
    {
        if (await db.QuestionCategories.AnyAsync()) return;

        db.QuestionCategories.Add(new QuestionCategory
        {
            Id = TestIds.SkillsCategoryId,
            Name = "Skills",
            CreatedById = TestIds.ManagerId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public static async Task<SurveyTemplate> SeedTemplateAsync(AppDbContext db)
    {
        if (await db.SurveyTemplates.AnyAsync(t => t.Id == TestIds.TemplateId))
            return await db.SurveyTemplates.Include(t => t.Questions).FirstAsync(t => t.Id == TestIds.TemplateId);

        var template = new SurveyTemplate
        {
            Id = TestIds.TemplateId,
            Name = "Standard 360",
            Description = "Test template",
            CreatedById = TestIds.ManagerId,
            Questions =
            [
                new TemplateQuestion
                {
                    Id = Guid.NewGuid(),
                    Order = 0,
                    Type = QuestionType.Rating,
                    Text = "Communicates effectively",
                    CategoryId = TestIds.SkillsCategoryId
                },
                new TemplateQuestion
                {
                    Id = Guid.NewGuid(),
                    Order = 1,
                    Type = QuestionType.OpenText,
                    Text = "Areas for improvement",
                    CategoryId = TestIds.SkillsCategoryId
                }
            ]
        };
        db.SurveyTemplates.Add(template);
        await db.SaveChangesAsync();
        return template;
    }

    public static async Task<Survey> SeedActiveSurveyAsync(AppDbContext db)
    {
        if (await db.Surveys.AnyAsync(s => s.Id == TestIds.SurveyId))
            return await db.Surveys.Include(s => s.Questions).Include(s => s.Assignments)
                .FirstAsync(s => s.Id == TestIds.SurveyId);

        var survey = new Survey
        {
            Id = TestIds.SurveyId,
            Title = "Alice 360 Review",
            SubjectEmployeeId = TestIds.Employee1Id,
            CreatedById = TestIds.ManagerId,
            Status = SurveyStatus.Active,
            DueDate = DateTime.UtcNow.AddDays(14),
            Questions =
            [
                new SurveyQuestion
                {
                    Id = TestIds.RatingQuestionId,
                    Order = 0,
                    Type = QuestionType.Rating,
                    Text = "Teamwork",
                    CategoryId = TestIds.SkillsCategoryId
                },
                new SurveyQuestion
                {
                    Id = TestIds.OpenTextQuestionId,
                    Order = 1,
                    Type = QuestionType.OpenText,
                    Text = "Comments",
                    CategoryId = TestIds.SkillsCategoryId
                }
            ],
            Assignments =
            [
                new SurveyAssignment
                {
                    Id = TestIds.AssignmentId,
                    ReviewerId = TestIds.ManagerId,
                    ReviewerType = ReviewerType.Manager,
                    Status = AssignmentStatus.Pending
                }
            ]
        };
        db.Surveys.Add(survey);
        await db.SaveChangesAsync();
        return survey;
    }

    public static async Task SeedAuditLogsAsync(AppDbContext db)
    {
        if (await db.AuditLogs.AnyAsync()) return;

        db.AuditLogs.AddRange(
            new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = TestIds.AdminId,
                Action = AuditAction.UserCreated,
                TargetUserId = TestIds.Employee1Id,
                Metadata = "{}",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = TestIds.AdminId,
                Action = AuditAction.UserUpdated,
                TargetUserId = TestIds.Employee2Id,
                Metadata = "{}",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
        await db.SaveChangesAsync();
    }

    public static async Task SeedAllAsync(AppDbContext db, IPasswordHasher? hasher = null)
    {
        await SeedUsersAsync(db, hasher);
        await SeedCategoryAsync(db);
        await SeedTemplateAsync(db);
    }
}
