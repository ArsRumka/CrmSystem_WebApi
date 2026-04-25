using Identity.Domain.Enums;

namespace Identity.Application.Contracts;

public sealed record SuccessResponse(bool Success);

public sealed record RequestIdResponse(Guid RequestId);

public sealed record RegisterOrganizationResponse(Guid OrganizationId);

public sealed record AuthTokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public sealed record AccessTokenResponse(string AccessToken, DateTime ExpiresAt);

public sealed record OrganizationRequestResponse(
    Guid Id,
    string CompanyName,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string? Comment,
    OrganizationRequestStatus Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    Guid? ProcessedBySystemAdminId);

public sealed record UserResponse(
    Guid Id,
    Guid OrganizationId,
    Guid RoleId,
    string Name,
    string Email,
    bool IsActive,
    bool IsEmailConfirmed);

public sealed record CurrentUserResponse(
    Guid UserId,
    Guid OrganizationId,
    string Name,
    string Email,
    RoleResponse Role,
    IReadOnlyList<RolePermissionResponse> Permissions);

public sealed record RoleResponse(Guid Id, Guid OrganizationId, string Name, IReadOnlyList<RolePermissionResponse> Permissions);

public sealed record RolePermissionResponse(
    Guid ModuleId,
    string ModuleCode,
    string ModuleName,
    bool CanRead,
    bool CanCreate,
    bool CanUpdate,
    bool CanDelete);

public sealed record ModuleResponse(Guid Id, string Code, string Name);

public sealed record RolePermissionRequest(
    string ModuleCode,
    bool CanRead,
    bool CanCreate,
    bool CanUpdate,
    bool CanDelete);
