using Microsoft.Extensions.DependencyInjection;

namespace Chat.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddChatPresentation(this IServiceCollection services)
    {
        return services;
    }
}
