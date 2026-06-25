namespace Feedback360.Domain.Entities;

public class Response
{
    public Guid Id { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid QuestionId { get; set; }
    public int? Rating { get; set; }
    public string? Comment { get; set; }
    public string? OpenText { get; set; }

    public SurveyAssignment Assignment { get; set; } = null!;
    public SurveyQuestion Question { get; set; } = null!;
}
