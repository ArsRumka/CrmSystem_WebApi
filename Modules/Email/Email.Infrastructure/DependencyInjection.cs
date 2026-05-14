using Email.Application.Abstractions.Repositories;
using Email.Application.Abstractions.Services;
using Email.Infrastructure.Configurations;
using Email.Infrastructure.Repositories;
using Email.Infrastructure.Services;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Email.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEmailInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EmailAutomationOptions>(configuration.GetSection("EmailAutomation"));

        services.AddDataProtection();

        services.AddSingleton<IEfConfigurationAssemblyProvider>(
            new EfConfigurationAssemblyProvider(typeof(EmailSettingsConfiguration).Assembly));

        services.AddScoped<IEmailSettingsRepository, EmailSettingsRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IEmailCampaignRepository, EmailCampaignRepository>();
        services.AddScoped<IEmailCampaignRecipientRepository, EmailCampaignRecipientRepository>();
        services.AddScoped<IEmailAutomationRuleRepository, EmailAutomationRuleRepository>();

        services.AddScoped<IEmailClientLookupService, EmailClientLookupService>();
        services.AddScoped<IEmailOrganizationLookupService, EmailOrganizationLookupService>();
        services.AddScoped<IEmailPasswordProtector, DataProtectionEmailPasswordProtector>();
        services.AddScoped<IOrganizationSmtpEmailSender, OrganizationSmtpEmailSender>();
        services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();
        services.AddScoped<IEmailCampaignSender, EmailCampaignSender>();
        services.AddScoped<IEmailAutomationRunner, EmailAutomationRunner>();

        services.AddHostedService<EmailAutomationHostedService>();

        return services;
    }
}
