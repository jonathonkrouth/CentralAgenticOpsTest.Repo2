using Ingredients.Application.Abstractions;
using Ingredients.Infrastructure.Data;
using Ingredients.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ingredients.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "IngredientsDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");
        }

        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddScoped<IIngredientRepository, IngredientRepository>();
        return services;
    }
}
