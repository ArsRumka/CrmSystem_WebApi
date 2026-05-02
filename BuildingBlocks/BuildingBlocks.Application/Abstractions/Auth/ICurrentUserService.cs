namespace BuildingBlocks.Application.Abstractions.Auth;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? OrganizationId { get; }
    Guid? RoleId { get; }
    bool IsAuthenticated { get; }
    bool IsSystemAdmin { get; }
    Guid? SystemAdminId { get; }
}
