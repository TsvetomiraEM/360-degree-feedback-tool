using Feedback360.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Feedback360.Application.Common;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<QuestionCategory> QuestionCategories { get; }
    DbSet<SurveyTemplate> SurveyTemplates { get; }
    DbSet<TemplateQuestion> TemplateQuestions { get; }
    DbSet<TemplateShare> TemplateShares { get; }
    DbSet<Survey> Surveys { get; }
    DbSet<SurveyQuestion> SurveyQuestions { get; }
    DbSet<SurveyAssignment> SurveyAssignments { get; }
    DbSet<Response> Responses { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
