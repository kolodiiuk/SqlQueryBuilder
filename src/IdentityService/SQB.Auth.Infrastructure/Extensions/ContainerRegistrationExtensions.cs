using Microsoft.Extensions.DependencyInjection;
using SQB.Auth.Domain.Interfaces;
using SQB.Auth.Infrastructure.Repositories;

namespace SQB.Auth.Infrastructure.Extensions;

public static class ContainerRegistrationExtensions
{
    public static void RegisterRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAuthRepository, AuthRepository>();
    }
}
