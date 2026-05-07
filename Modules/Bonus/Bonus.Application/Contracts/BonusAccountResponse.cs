using Bonus.Domain.Entities;

namespace Bonus.Application.Contracts;

public sealed record BonusAccountResponse(
    Guid Id,
    Guid OrganizationId,
    Guid ClientId,
    decimal Balance,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

internal static class BonusAccountResponseMapper
{
    public static BonusAccountResponse ToResponse(this BonusAccount account)
    {
        return new BonusAccountResponse(
            account.Id,
            account.OrganizationId,
            account.ClientId,
            account.Balance,
            account.IsActive,
            account.CreatedAt,
            account.UpdatedAt);
    }
}
