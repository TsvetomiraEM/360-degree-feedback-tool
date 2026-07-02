namespace Feedback360.Domain.Entities;

public class SurveyTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User CreatedBy { get; set; } = null!;
    public ICollection<TemplateQuestion> Questions { get; set; } = new List<TemplateQuestion>();
    public ICollection<TemplateShare> Shares { get; set; } = new List<TemplateShare>();
}
