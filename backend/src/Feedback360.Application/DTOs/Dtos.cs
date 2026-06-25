using Feedback360.Domain.Enums;

namespace Feedback360.Application.DTOs;

public record UserDto(Guid Id, string Email, string Name, UserRole Role, Guid? ManagerId, string? ManagerName, bool IsActive, string AuthProvider);
public record CreateUserRequest(string Email, string Name, UserRole Role, Guid? ManagerId, string? Password);
public record UpdateUserRequest(string Email, string Name, UserRole Role, Guid? ManagerId, string? Password);
public record SetActiveRequest(bool IsActive);

public record AuthResponse(string AccessToken, string RefreshToken, UserDto User);
public record LoginRequest(string Email, string Password);
public record GoogleLoginRequest(string IdToken);
public record RefreshRequest(string RefreshToken);

public record AuditLogDto(Guid Id, Guid ActorUserId, string ActorName, AuditAction Action, Guid? TargetUserId, string? TargetUserName, string Metadata, DateTime CreatedAt);
public record AuditLogQuery(int Page = 1, int PageSize = 20, AuditAction? Action = null, DateTime? From = null, DateTime? To = null);
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public record QuestionCategoryDto(Guid Id, string Name, string CreatedByName, DateTime CreatedAt);
public record CreateQuestionCategoryRequest(string Name);

public record QuestionInput(QuestionType Type, string Text, int Order, Guid CategoryId, string? CategoryName = null);
public record TemplateDto(Guid Id, string Name, string? Description, Guid CreatedById, string CreatedByName, DateTime CreatedAt, List<QuestionInput> Questions, bool IsOwner, bool IsShared);
public record CreateTemplateRequest(string Name, string? Description, List<QuestionInput> Questions);
public record UpdateTemplateRequest(string Name, string? Description, List<QuestionInput> Questions);
public record ShareTemplateRequest(List<Guid> ManagerIds);

public record SurveyDto(Guid Id, string Title, Guid SubjectEmployeeId, string SubjectEmployeeName, Guid CreatedById, SurveyStatus Status, DateTime? DueDate, bool ResultsPublished, DateTime CreatedAt, int AssignmentCount, int CompletedCount);
public record SurveyDetailDto(Guid Id, string Title, Guid SubjectEmployeeId, string SubjectEmployeeName, SurveyStatus Status, DateTime? DueDate, bool ResultsPublished, List<QuestionInput> Questions, List<AssignmentDto> Assignments);
public record CreateSurveyRequest(string Title, Guid SubjectEmployeeId, DateTime? DueDate, List<QuestionInput> Questions);
public record CreateSurveyFromTemplateRequest(Guid SubjectEmployeeId, DateTime? DueDate, string? Title);
public record AssignSurveyRequest(List<Guid> PeerIds);

public record AssignmentDto(Guid Id, Guid SurveyId, string SurveyTitle, string SubjectEmployeeName, ReviewerType ReviewerType, AssignmentStatus Status, DateTime? DueDate, DateTime? CompletedAt);
public record AssignmentDetailDto(Guid Id, Guid SurveyId, string SurveyTitle, string SubjectEmployeeName, ReviewerType ReviewerType, AssignmentStatus Status, List<QuestionForResponseDto> Questions, List<ResponseInput>? ExistingResponses);
public record QuestionForResponseDto(Guid Id, QuestionType Type, string Text, int Order, Guid CategoryId, string CategoryName);
public record ResponseInput(Guid QuestionId, int? Rating, string? Comment, string? OpenText);
public record SubmitResponsesRequest(List<ResponseInput> Responses);

public record ResultsDto(Guid SurveyId, string Title, string SubjectEmployeeName, bool ResultsPublished, List<string> Labels, List<ResultsSeriesDto> Series, List<ResultsCommentGroupDto> CommentGroups, List<OpenTextGroupDto> OpenTextGroups);
public record ResultsSeriesDto(string Name, List<double?> Data);
public record ResultsCommentGroupDto(string ReviewerType, string QuestionText, List<string> Comments);
public record OpenTextGroupDto(string ReviewerType, string QuestionText, List<string> Responses);
