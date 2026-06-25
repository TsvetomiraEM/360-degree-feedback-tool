using Feedback360.Domain.Enums;

namespace Feedback360.Domain.Entities;

public class SurveyQuestion
{
    public Guid Id { get; set; }
    public Guid SurveyId { get; set; }
    public int Order { get; set; }
    public QuestionType Type { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }

    public Survey Survey { get; set; } = null!;
    public QuestionCategory Category { get; set; } = null!;
    public ICollection<Response> Responses { get; set; } = new List<Response>();
}
