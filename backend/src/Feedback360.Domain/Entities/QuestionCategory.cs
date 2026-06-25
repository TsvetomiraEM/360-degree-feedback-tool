namespace Feedback360.Domain.Entities;

public class QuestionCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User CreatedBy { get; set; } = null!;
    public ICollection<TemplateQuestion> TemplateQuestions { get; set; } = new List<TemplateQuestion>();
    public ICollection<SurveyQuestion> SurveyQuestions { get; set; } = new List<SurveyQuestion>();
}
