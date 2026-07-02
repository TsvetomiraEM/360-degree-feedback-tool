namespace Feedback360.Domain.Entities;

public class TemplateShare
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public Guid SharedWithManagerId { get; set; }

    public SurveyTemplate Template { get; set; } = null!;
    public User SharedWithManager { get; set; } = null!;
}
