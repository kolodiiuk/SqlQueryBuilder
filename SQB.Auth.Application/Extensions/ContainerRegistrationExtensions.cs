using Microsoft.Extensions.DependencyInjection;
using SQB.Auth.Application.Services;
using SQB.Auth.Domain.Interfaces;

namespace SQB.Auth.Application.Extensions;

public static class ContainerRegistrationExtensions
{
    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtService, JwtService>();
    }
}
