using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Feedback360.Application.DTOs;
using Feedback360.Application.Tests.Helpers;
using Feedback360.Domain.Entities;
using Feedback360.Domain.Enums;
using Feedback360.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Feedback360.Api.Tests;

public class ApiIntegrationTests : IClassFixture<Feedback360WebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly Feedback360WebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public ApiIntegrationTests(Feedback360WebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_Returns200_WithValidCredentials()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(
            "manager@feedback360.local", "Manager123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.User.Role.Should().Be(UserRole.Manager);
    }

    [Fact]
    public async Task Admin_GetResults_Returns403()
    {
        var token = await LoginAsync("admin@feedback360.local", "Admin123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/results");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Manager_GetResults_Returns200()
    {
        var token = await LoginAsync("manager@feedback360.local", "Manager123!");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/results");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var surveys = await response.Content.ReadFromJsonAsync<List<SurveyDto>>(JsonOptions);
        surveys.Should().NotBeNull();
    }

    [Fact]
    public async Task Manager_PostAdminUsers_Returns403()
    {
        var token = await LoginAsync("manager@feedback360.local", "Manager123!");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new CreateUserRequest(
            "new@feedback360.local", "New User", UserRole.Employee, null, "Employee123!"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CategoryCreate_DeduplicatesByName()
    {
        var token = await LoginAsync("manager@feedback360.local", "Manager123!");
        var name = $"E2E-Category-{Guid.NewGuid():N}";

        var first = await PostCategoryAsync(token, name);
        var second = await PostCategoryAsync(token, name);

        first.Id.Should().Be(second.Id);
        first.Name.Should().Be(name);
    }

    [Fact]
    public async Task UserDelete_CascadesRelatedData()
    {
        var adminToken = await LoginAsync("admin@feedback360.local", "Admin123!");
        var employeeId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestData.SeedUsersAsync(db);
            await TestData.SeedCategoryAsync(db);

            db.Users.Add(new User
            {
                Id = employeeId,
                Email = $"cascade-{employeeId:N}@feedback360.local",
                Name = "Cascade Test Employee",
                Role = UserRole.Employee,
                ManagerId = TestIds.ManagerId,
                IsActive = true,
                AuthProvider = "local",
                PasswordHash = "hash"
            });

            var survey = new Survey
            {
                Id = Guid.NewGuid(),
                Title = "Cascade Survey",
                SubjectEmployeeId = employeeId,
                CreatedById = TestIds.ManagerId,
                Status = SurveyStatus.Active,
                Questions =
                [
                    new SurveyQuestion
                    {
                        Id = Guid.NewGuid(),
                        Order = 0,
                        Type = QuestionType.Rating,
                        Text = "Rating Q",
                        CategoryId = TestIds.SkillsCategoryId
                    }
                ],
                Assignments =
                [
                    new SurveyAssignment
                    {
                        Id = Guid.NewGuid(),
                        ReviewerId = employeeId,
                        ReviewerType = ReviewerType.Peer,
                        Status = AssignmentStatus.Pending
                    }
                ]
            };
            db.Surveys.Add(survey);

            db.SurveyTemplates.Add(new SurveyTemplate
            {
                Id = Guid.NewGuid(),
                Name = "Cascade Template",
                CreatedById = employeeId,
                Questions =
                [
                    new TemplateQuestion
                    {
                        Id = Guid.NewGuid(),
                        Order = 0,
                        Type = QuestionType.Rating,
                        Text = "Q1",
                        CategoryId = TestIds.SkillsCategoryId
                    }
                ]
            });

            await db.SaveChangesAsync();
        }

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/users/{employeeId}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db2.Users.AnyAsync(u => u.Id == employeeId)).Should().BeFalse();
        (await db2.Surveys.AnyAsync(s => s.SubjectEmployeeId == employeeId)).Should().BeFalse();
        (await db2.SurveyAssignments.AnyAsync(a => a.ReviewerId == employeeId)).Should().BeFalse();
        (await db2.SurveyTemplates.AnyAsync(t => t.CreatedById == employeeId)).Should().BeFalse();
    }

    [Fact]
    public async Task Health_Returns200_WithoutAuthentication()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        return auth!.AccessToken;
    }

    private async Task<QuestionCategoryDto> PostCategoryAsync(string token, string name)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/categories");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new CreateQuestionCategoryRequest(name));

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<QuestionCategoryDto>(JsonOptions))!;
    }
}
