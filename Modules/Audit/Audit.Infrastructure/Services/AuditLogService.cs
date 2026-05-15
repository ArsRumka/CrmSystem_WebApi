using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Audit.Application.Abstractions.Repositories;
using Audit.Application.Abstractions.Services;
using Audit.Domain.Entities;
using BuildingBlocks.Application.Abstractions.Time;

namespace Audit.Infrastructure.Services;

public sealed class AuditLogService : IAuditLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private static readonly string[] SensitiveKeyParts =
    [
        "password",
        "token",
        "key",
        "secret",
        "authorization"
    ];

    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuditLogService(
        IAuditLogRepository auditLogRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _auditLogRepository = auditLogRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task LogAsync(
        Guid organizationId,
        Guid? userId,
        string moduleCode,
        Audit.Domain.Enums.AuditAction action,
        string entityName,
        Guid? entityId,
        string description,
        object? oldValues,
        object? newValues,
        CancellationToken cancellationToken)
    {
        return LogAsync(
            new AuditLogCreateRequest(
                organizationId,
                userId,
                moduleCode,
                action,
                entityName,
                entityId,
                description,
                oldValues,
                newValues),
            cancellationToken);
    }

    public async Task LogAsync(AuditLogCreateRequest request, CancellationToken cancellationToken)
    {
        var log = new AuditLog(
            Guid.NewGuid(),
            request.OrganizationId,
            request.UserId,
            request.ModuleCode,
            request.Action,
            request.EntityName,
            request.EntityId,
            request.Description,
            SerializeSafe(request.OldValues),
            SerializeSafe(request.NewValues),
            _dateTimeProvider.UtcNow,
            request.IpAddress,
            request.UserAgent,
            request.CorrelationId);

        await _auditLogRepository.AddAsync(log, cancellationToken);
    }

    private static string? SerializeSafe(object? values)
    {
        if (values is null)
        {
            return null;
        }

        try
        {
            var node = JsonSerializer.SerializeToNode(values, JsonOptions);
            if (node is null)
            {
                return null;
            }

            Sanitize(node);
            return node.ToJsonString(JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static void Sanitize(JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            var keysToRemove = jsonObject
                .Where(property => IsSensitiveKey(property.Key))
                .Select(property => property.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                jsonObject.Remove(key);
            }

            foreach (var child in jsonObject.Select(property => property.Value).OfType<JsonNode>())
            {
                Sanitize(child);
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var child in jsonArray.OfType<JsonNode>())
            {
                Sanitize(child);
            }
        }
    }

    private static bool IsSensitiveKey(string key)
    {
        return SensitiveKeyParts.Any(part =>
            key.Contains(part, StringComparison.OrdinalIgnoreCase));
    }
}

