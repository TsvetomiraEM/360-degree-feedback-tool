using Feedback360.Application.Common;
using Feedback360.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Feedback360.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<QuestionCategory> QuestionCategories => Set<QuestionCategory>();
    public DbSet<SurveyTemplate> SurveyTemplates => Set<SurveyTemplate>();
    public DbSet<TemplateQuestion> TemplateQuestions => Set<TemplateQuestion>();
    public DbSet<TemplateShare> TemplateShares => Set<TemplateShare>();
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<SurveyQuestion> SurveyQuestions => Set<SurveyQuestion>();
    public DbSet<SurveyAssignment> SurveyAssignments => Set<SurveyAssignment>();
    public DbSet<Response> Responses => Set<Response>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Manager).WithMany(u => u.DirectReports).HasForeignKey(u => u.ManagerId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasOne(a => a.Actor).WithMany(u => u.AuditLogs).HasForeignKey(a => a.ActorUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.TargetUser).WithMany().HasForeignKey(a => a.TargetUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<QuestionCategory>(e =>
        {
            e.HasIndex(c => c.Name).IsUnique();
            e.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SurveyTemplate>(e =>
        {
            e.HasOne(t => t.CreatedBy).WithMany(u => u.Templates).HasForeignKey(t => t.CreatedById).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TemplateQuestion>(e =>
        {
            e.HasOne(q => q.Template).WithMany(t => t.Questions).HasForeignKey(q => q.TemplateId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(q => q.Category).WithMany(c => c.TemplateQuestions).HasForeignKey(q => q.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TemplateShare>(e =>
        {
            e.HasOne(s => s.Template).WithMany(t => t.Shares).HasForeignKey(s => s.TemplateId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.SharedWithManager).WithMany(u => u.SharedTemplates).HasForeignKey(s => s.SharedWithManagerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Survey>(e =>
        {
            e.HasIndex(s => s.SubjectEmployeeId);
            e.HasOne(s => s.SubjectEmployee).WithMany(u => u.SubjectSurveys).HasForeignKey(s => s.SubjectEmployeeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.CreatedBy).WithMany(u => u.CreatedSurveys).HasForeignKey(s => s.CreatedById).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Template).WithMany().HasForeignKey(s => s.TemplateId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SurveyQuestion>(e =>
        {
            e.HasOne(q => q.Survey).WithMany(s => s.Questions).HasForeignKey(q => q.SurveyId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(q => q.Category).WithMany(c => c.SurveyQuestions).HasForeignKey(q => q.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SurveyAssignment>(e =>
        {
            e.HasIndex(a => a.ReviewerId);
            e.HasIndex(a => a.SurveyId);
            e.HasOne(a => a.Survey).WithMany(s => s.Assignments).HasForeignKey(a => a.SurveyId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Reviewer).WithMany(u => u.Assignments).HasForeignKey(a => a.ReviewerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Response>(e =>
        {
            e.HasOne(r => r.Assignment).WithMany(a => a.Responses).HasForeignKey(r => r.AssignmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Question).WithMany(q => q.Responses).HasForeignKey(r => r.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
