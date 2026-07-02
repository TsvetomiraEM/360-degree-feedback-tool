using Feedback360.Domain.Enums;

namespace Feedback360.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? ManagerId { get; set; }
    public bool IsActive { get; set; } = true;
    public string AuthProvider { get; set; } = "local";
    public string? PasswordHash { get; set; }

    public User? Manager { get; set; }
    public ICollection<User> DirectReports { get; set; } = new List<User>();
    public ICollection<Survey> CreatedSurveys { get; set; } = new List<Survey>();
    public ICollection<Survey> SubjectSurveys { get; set; } = new List<Survey>();
    public ICollection<SurveyAssignment> Assignments { get; set; } = new List<SurveyAssignment>();
    public ICollection<SurveyTemplate> Templates { get; set; } = new List<SurveyTemplate>();
    public ICollection<TemplateShare> SharedTemplates { get; set; } = new List<TemplateShare>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
