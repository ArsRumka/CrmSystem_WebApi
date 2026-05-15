using System.Net.Http.Headers;
using System.Net.Http.Json;
using Identity.Application.Abstractions.Security;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using IdentityModule = Identity.Domain.Entities.Module;

namespace CrmSystem.ApiTests.Infrastructure;

public sealed class AuthTestClient
{
    private const string DefaultPassword = "Password123!";
    private readonly CrmWebApplicationFactory _factory;

    public AuthTestClient(CrmWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task<TestUser> CreateOrganizationWithAdminAsync(string? label = null)
    {
        return CreateOrganizationUserAsync(
            roleName: "Admin",
            permissionFactory: _ => ModulePermissions.All,
            label);
    }

    public async Task<TestUser> CreateUserAsync(
        TestUser organizationUser,
        string? name = null,
        string? email = null,
        bool grantAllPermissions = true)
    {
        var suffix = TestData.UniqueSuffix();
        var roleName = $"User-{suffix}";
        var userName = name ?? $"User {suffix}";
        var userEmail = email ?? $"user-{suffix}@example.test";

        return await _factory.ExecuteScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var passwordHasher = services.GetRequiredService<IPasswordHasher>();
            var modules = await GetModulesAsync(dbContext);

            var role = new Role(Guid.NewGuid(), organizationUser.OrganizationId, roleName);
            var user = new User(
                Guid.NewGuid(),
                organizationUser.OrganizationId,
                role.Id,
                userName,
                userEmail,
                passwordHasher.Hash(DefaultPassword));
            user.ConfirmEmail();

            dbContext.Set<Role>().Add(role);
            dbContext.Set<User>().Add(user);
            dbContext.Set<ModuleRole>().AddRange(modules.Select(module =>
            {
                var permissions = grantAllPermissions ? ModulePermissions.All : ModulePermissions.None;
                return CreateModuleRole(organizationUser.OrganizationId, role.Id, module, permissions);
            }));

            await dbContext.SaveChangesAsync();

            return new TestUser(
                organizationUser.OrganizationId,
                user.Id,
                role.Id,
                organizationUser.OrganizationEmail,
                user.Email,
                DefaultPassword);
        });
    }

    public async Task<AuthenticatedTestClient> CreateLimitedUserClientAsync(
        string deniedModuleCode,
        PermissionAction deniedAction)
    {
        var user = await CreateOrganizationUserAsync(
            roleName: "Limited",
            permissionFactory: module =>
            {
                var permissions = ModulePermissions.All;
                if (module.Code == deniedModuleCode)
                {
                    permissions = permissions.WithDenied(deniedAction);
                }

                return permissions;
            },
            label: null);

        return new AuthenticatedTestClient(user, await CreateAuthenticatedClientAsync(user));
    }

    public Task<TestUser> SeedSecondOrganizationAsync(string? label = null)
    {
        return CreateOrganizationWithAdminAsync(label ?? "second");
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(TestUser user)
    {
        var client = _factory.CreateClient(CrmWebApplicationFactory.DefaultClientOptions);
        var loginResponse = await client.PostAsJsonAsync("/api/identity/login", new
        {
            organizationEmail = user.OrganizationEmail,
            userEmail = user.UserEmail,
            password = user.Password
        });

        await loginResponse.AssertSuccessAsync();
        using var json = await loginResponse.ReadJsonDocumentAsync();
        var accessToken = json.RootElement.GetString("accessToken");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private async Task<TestUser> CreateOrganizationUserAsync(
        string roleName,
        Func<IdentityModule, ModulePermissions> permissionFactory,
        string? label)
    {
        var suffix = TestData.UniqueSuffix(label);
        var organizationEmail = $"org-{suffix}@example.test";
        var userEmail = $"admin-{suffix}@example.test";

        return await _factory.ExecuteScopeAsync(async services =>
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var passwordHasher = services.GetRequiredService<IPasswordHasher>();
            var modules = await GetModulesAsync(dbContext);

            var organization = new Organization(
                Guid.NewGuid(),
                $"Organization {suffix}",
                organizationEmail,
                $"license-{suffix}");
            var role = new Role(Guid.NewGuid(), organization.Id, roleName);
            var user = new User(
                Guid.NewGuid(),
                organization.Id,
                role.Id,
                $"Admin {suffix}",
                userEmail,
                passwordHasher.Hash(DefaultPassword));
            user.ConfirmEmail();

            dbContext.Set<Organization>().Add(organization);
            dbContext.Set<Role>().Add(role);
            dbContext.Set<User>().Add(user);
            dbContext.Set<ModuleRole>().AddRange(modules.Select(module =>
                CreateModuleRole(organization.Id, role.Id, module, permissionFactory(module))));

            await dbContext.SaveChangesAsync();

            return new TestUser(
                organization.Id,
                user.Id,
                role.Id,
                organization.Email,
                user.Email,
                DefaultPassword);
        });
    }

    private static async Task<List<IdentityModule>> GetModulesAsync(ApplicationDbContext dbContext)
    {
        var modules = await dbContext.Set<IdentityModule>()
            .OrderBy(module => module.Code)
            .ToListAsync();

        if (modules.Count == 0)
        {
            throw new InvalidOperationException("Identity modules were not seeded.");
        }

        return modules;
    }

    private static ModuleRole CreateModuleRole(
        Guid organizationId,
        Guid roleId,
        IdentityModule module,
        ModulePermissions permissions)
    {
        return new ModuleRole(
            Guid.NewGuid(),
            organizationId,
            roleId,
            module.Id,
            permissions.CanRead,
            permissions.CanCreate,
            permissions.CanUpdate,
            permissions.CanDelete);
    }

    private sealed record ModulePermissions(
        bool CanRead,
        bool CanCreate,
        bool CanUpdate,
        bool CanDelete)
    {
        public static ModulePermissions All { get; } = new(true, true, true, true);

        public static ModulePermissions None { get; } = new(false, false, false, false);

        public ModulePermissions WithDenied(PermissionAction action)
        {
            return action switch
            {
                PermissionAction.Read => this with { CanRead = false },
                PermissionAction.Create => this with { CanCreate = false },
                PermissionAction.Update => this with { CanUpdate = false },
                PermissionAction.Delete => this with { CanDelete = false },
                _ => this
            };
        }
    }
}

public sealed record TestUser(
    Guid OrganizationId,
    Guid UserId,
    Guid RoleId,
    string OrganizationEmail,
    string UserEmail,
    string Password);

public sealed record AuthenticatedTestClient(TestUser User, HttpClient Client) : IDisposable
{
    public void Dispose()
    {
        Client.Dispose();
    }
}
