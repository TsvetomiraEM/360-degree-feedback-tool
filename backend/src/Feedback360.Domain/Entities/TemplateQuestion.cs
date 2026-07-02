using Feedback360.Domain.Enums;

namespace Feedback360.Domain.Entities;

public class TemplateQuestion
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public int Order { get; set; }
    public QuestionType Type { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }

    public SurveyTemplate Template { get; set; } = null!;
    public QuestionCategory Category { get; set; } = null!;
}
