using Feedback360.Domain.Enums;

namespace Feedback360.Domain.Entities;

public class SurveyAssignment
{
    public Guid Id { get; set; }
    public Guid SurveyId { get; set; }
    public Guid ReviewerId { get; set; }
    public ReviewerType ReviewerType { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Pending;
    public DateTime? CompletedAt { get; set; }

    public Survey Survey { get; set; } = null!;
    public User Reviewer { get; set; } = null!;
    public ICollection<Response> Responses { get; set; } = new List<Response>();
}
