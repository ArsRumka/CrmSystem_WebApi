using Audit.Domain.Enums;

namespace Audit.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? UserId { get; private set; }
    public string ModuleCode { get; private set; } = null!;
    public AuditAction Action { get; private set; }
    public string EntityName { get; private set; } = null!;
    public Guid? EntityId { get; private set; }
    public string Description { get; private set; } = null!;
    public string? OldValuesJson { get; private set; }
    public string? NewValuesJson { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? CorrelationId { get; private set; }

    private AuditLog()
    {
    }

    public AuditLog(
        Guid id,
        Guid organizationId,
        Guid? userId,
        string moduleCode,
        AuditAction action,
        string entityName,
        Guid? entityId,
        string description,
        string? oldValuesJson,
        string? newValuesJson,
        DateTime createdAt,
        string? ipAddress = null,
        string? userAgent = null,
        string? correlationId = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        if (!Enum.IsDefined(action))
            throw new ArgumentException("Invalid audit action", nameof(action));

        if (entityId == Guid.Empty)
            throw new ArgumentException("EntityId cannot be empty", nameof(entityId));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        Id = id;
        OrganizationId = organizationId;
        UserId = userId;
        ModuleCode = Require(moduleCode, nameof(moduleCode), 100);
        Action = action;
        EntityName = Require(entityName, nameof(entityName), 200);
        EntityId = entityId;
        Description = Require(description, nameof(description), 1000);
        OldValuesJson = NormalizeOptional(oldValuesJson);
        NewValuesJson = NormalizeOptional(newValuesJson);
        CreatedAt = createdAt;
        IpAddress = NormalizeOptional(ipAddress, nameof(ipAddress), 100);
        UserAgent = NormalizeOptional(userAgent, nameof(userAgent), 500);
        CorrelationId = NormalizeOptional(correlationId, nameof(correlationId), 100);
    }

    private static string Require(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} is required", parameterName);

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters", parameterName);

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters", parameterName);

        return normalized;
    }
}

