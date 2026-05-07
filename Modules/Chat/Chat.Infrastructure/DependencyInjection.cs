using Chat.Application.Abstractions.Lookups;
using Chat.Application.Abstractions.Repositories;
using Chat.Infrastructure.Configurations;
using Chat.Infrastructure.Lookups;
using Chat.Infrastructure.Repositories;
using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Chat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddChatInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEfConfigurationAssemblyProvider>(
            new EfConfigurationAssemblyProvider(typeof(ChatConversationConfiguration).Assembly));

        services.AddScoped<IChatConversationRepository, ChatConversationRepository>();
        services.AddScoped<IChatParticipantRepository, ChatParticipantRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IChatContactRequestRepository, ChatContactRequestRepository>();

        services.AddScoped<IChatUserLookupService, ChatUserLookupService>();
        services.AddScoped<IChatOrganizationLookupService, ChatOrganizationLookupService>();
        services.AddScoped<IChatClientLookupService, ChatClientLookupService>();
        services.AddScoped<IChatDealLookupService, ChatDealLookupService>();

        return services;
    }
}
