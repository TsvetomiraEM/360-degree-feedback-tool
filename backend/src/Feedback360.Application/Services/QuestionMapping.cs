using Feedback360.Application.DTOs;
using Feedback360.Domain.Entities;

namespace Feedback360.Application.Services;

internal static class QuestionMapping
{
    public static QuestionInput ToQuestionInput(TemplateQuestion q) =>
        new(q.Type, q.Text, q.Order, q.CategoryId, q.Category?.Name);

    public static QuestionInput ToQuestionInput(SurveyQuestion q) =>
        new(q.Type, q.Text, q.Order, q.CategoryId, q.Category?.Name);

    public static QuestionForResponseDto ToResponseDto(SurveyQuestion q) =>
        new(q.Id, q.Type, q.Text, q.Order, q.CategoryId, q.Category?.Name ?? string.Empty);
}
