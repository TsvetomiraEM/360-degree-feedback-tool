using Feedback360.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Feedback360.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<AuditLogQueryService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<TemplateService>();
        services.AddScoped<SurveyService>();
        services.AddScoped<AssignmentService>();
        services.AddScoped<ResultsService>();
        return services;
    }
}
