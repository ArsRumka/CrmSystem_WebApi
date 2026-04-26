using Clients.Domain.Entities;
using Clients.Domain.Enums;

namespace Clients.Application.Contracts;

public sealed record ClientResponse(
    Guid Id,
    Guid OrganizationId,
    string FirstName,
    string LastName,
    string? MiddleName,
    string FullName,
    string? Email,
    string? Phone,
    ClientStatus Status,
    ClientSource Source,
    bool AllowMarketingEmails,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

internal static class ClientResponseMapper
{
    public static ClientResponse ToResponse(this Client client)
    {
        var fullName = string.IsNullOrWhiteSpace(client.MiddleName)
            ? $"{client.LastName} {client.FirstName}"
            : $"{client.LastName} {client.FirstName} {client.MiddleName}";

        return new ClientResponse(
            client.Id,
            client.OrganizationId,
            client.FirstName,
            client.LastName,
            client.MiddleName,
            fullName,
            client.Email,
            client.Phone,
            client.Status,
            client.Source,
            client.AllowMarketingEmails,
            client.Notes,
            client.IsActive,
            client.CreatedAt,
            client.UpdatedAt);
    }
}
