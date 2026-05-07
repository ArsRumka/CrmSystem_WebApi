namespace Bonus.Domain.Entities;

public class BonusAccount
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid ClientId { get; private set; }
    public decimal Balance { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private BonusAccount()
    {
    }

    public BonusAccount(Guid id, Guid organizationId, Guid clientId, DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (clientId == Guid.Empty)
            throw new ArgumentException("ClientId is required", nameof(clientId));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        Id = id;
        OrganizationId = organizationId;
        ClientId = clientId;
        Balance = 0;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public void Increase(decimal points, DateTime updatedAt)
    {
        ValidatePositivePoints(points);
        ValidateTimestamp(updatedAt, nameof(updatedAt));

        Balance += points;
        UpdatedAt = updatedAt;
    }

    public void Decrease(decimal points, DateTime updatedAt)
    {
        ValidatePositivePoints(points);
        ValidateTimestamp(updatedAt, nameof(updatedAt));

        if (Balance - points < 0)
            throw new InvalidOperationException("Bonus balance cannot become negative");

        Balance -= points;
        UpdatedAt = updatedAt;
    }

    public void Correct(decimal newBalance, DateTime updatedAt)
    {
        if (newBalance < 0)
            throw new ArgumentException("Balance must be greater than or equal to zero", nameof(newBalance));

        ValidateTimestamp(updatedAt, nameof(updatedAt));

        Balance = newBalance;
        UpdatedAt = updatedAt;
    }

    public void Deactivate(DateTime updatedAt)
    {
        ValidateTimestamp(updatedAt, nameof(updatedAt));

        IsActive = false;
        UpdatedAt = updatedAt;
    }

    private static void ValidatePositivePoints(decimal points)
    {
        if (points <= 0)
            throw new ArgumentException("Points must be greater than zero", nameof(points));
    }

    private static void ValidateTimestamp(DateTime value, string parameterName)
    {
        if (value == default)
            throw new ArgumentException($"{parameterName} is required", parameterName);
    }
}
