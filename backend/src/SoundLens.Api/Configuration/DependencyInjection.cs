using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Api.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add application services and configurations here
        services.AddSingleton<IImportedFileStore, InMemoryImportedFileStore>();

        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add infrastructure services and configurations here

        return services;
    }
}
