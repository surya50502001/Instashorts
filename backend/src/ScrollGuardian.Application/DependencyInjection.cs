using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ScrollGuardian.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
