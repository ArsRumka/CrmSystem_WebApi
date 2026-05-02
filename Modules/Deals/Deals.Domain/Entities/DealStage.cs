namespace Deals.Domain.Entities;

public class DealStage
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = null!;
    public int Order { get; private set; }
    public bool IsFinal { get; private set; }
    public bool IsSuccessful { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private DealStage()
    {
    }

    public DealStage(
        Guid id,
        Guid organizationId,
        string name,
        int order,
        bool isFinal,
        bool isSuccessful,
        DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        Id = id;
        OrganizationId = organizationId;
        Name = Require(name, nameof(name));
        Order = RequirePositiveOrder(order);
        IsFinal = isFinal;
        IsSuccessful = isSuccessful;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public void Update(
        string name,
        int order,
        bool isFinal,
        bool isSuccessful,
        DateTime updatedAt)
    {
        if (updatedAt == default)
            throw new ArgumentException("UpdatedAt is required", nameof(updatedAt));

        Name = Require(name, nameof(name));
        Order = RequirePositiveOrder(order);
        IsFinal = isFinal;
        IsSuccessful = isSuccessful;
        UpdatedAt = updatedAt;
    }

    public void Deactivate(DateTime updatedAt)
    {
        if (updatedAt == default)
            throw new ArgumentException("UpdatedAt is required", nameof(updatedAt));

        IsActive = false;
        UpdatedAt = updatedAt;
    }

    private static string Require(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} is required", parameterName);

        return value.Trim();
    }

    private static int RequirePositiveOrder(int order)
    {
        if (order <= 0)
            throw new ArgumentException("Order must be greater than zero", nameof(order));

        return order;
    }
}
