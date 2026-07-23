using Carter;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation;

public static class PresentationDependencyInjection
{
    public static void AddPresentationDependencyInjection(this IServiceCollection services)
    {
        services.AddCarter();
    }
}
