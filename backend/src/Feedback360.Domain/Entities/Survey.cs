using Feedback360.Domain.Enums;

namespace Feedback360.Domain.Entities;

public class Survey
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? TemplateId { get; set; }
    public Guid SubjectEmployeeId { get; set; }
    public Guid CreatedById { get; set; }
    public SurveyStatus Status { get; set; } = SurveyStatus.Draft;
    public DateTime? DueDate { get; set; }
    public bool ResultsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SurveyTemplate? Template { get; set; }
    public User SubjectEmployee { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
    public ICollection<SurveyAssignment> Assignments { get; set; } = new List<SurveyAssignment>();
}
