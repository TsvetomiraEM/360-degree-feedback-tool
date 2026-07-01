using Feedback360.Application.DTOs;
using Feedback360.Application.Services;
using Feedback360.Domain.Entities;
using Feedback360.Domain.Enums;
using Feedback360.Application.Tests.Helpers;
using FluentAssertions;

namespace Feedback360.Application.Tests.Services;

public class QuestionMappingTests
{
    [Fact]
    public void ToQuestionInput_FromTemplateQuestion_MapsCategoryName()
    {
        var question = new TemplateQuestion
        {
            Type = QuestionType.Rating,
            Text = "Q1",
            Order = 1,
            CategoryId = TestIds.SkillsCategoryId,
            Category = new QuestionCategory { Name = "Skills" }
        };

        var result = QuestionMapping.ToQuestionInput(question);
        result.CategoryName.Should().Be("Skills");
    }

    [Fact]
    public void ToResponseDto_FromSurveyQuestion_UsesEmptyCategoryWhenMissing()
    {
        var question = new SurveyQuestion
        {
            Id = Guid.NewGuid(),
            Type = QuestionType.OpenText,
            Text = "Open",
            Order = 0,
            CategoryId = TestIds.SkillsCategoryId
        };

        var result = QuestionMapping.ToResponseDto(question);
        result.CategoryName.Should().BeEmpty();
    }
}
