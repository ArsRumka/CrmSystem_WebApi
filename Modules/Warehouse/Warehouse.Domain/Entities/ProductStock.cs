namespace Warehouse.Domain.Entities;

public class ProductStock
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid StorageId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private ProductStock()
    {
    }

    public ProductStock(
        Guid id,
        Guid organizationId,
        Guid storageId,
        Guid productId,
        decimal quantity,
        DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (storageId == Guid.Empty)
            throw new ArgumentException("StorageId is required", nameof(storageId));

        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId is required", nameof(productId));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        ValidateQuantity(quantity, nameof(quantity));

        Id = id;
        OrganizationId = organizationId;
        StorageId = storageId;
        ProductId = productId;
        Quantity = quantity;
        CreatedAt = createdAt;
    }

    public void Increase(decimal quantity, DateTime updatedAt)
    {
        ValidatePositiveQuantity(quantity, nameof(quantity));
        RequireDate(updatedAt, nameof(updatedAt));

        Quantity += quantity;
        UpdatedAt = updatedAt;
    }

    public void Decrease(decimal quantity, DateTime updatedAt)
    {
        ValidatePositiveQuantity(quantity, nameof(quantity));
        RequireDate(updatedAt, nameof(updatedAt));

        if (Quantity < quantity)
            throw new InvalidOperationException("Insufficient stock quantity");

        Quantity -= quantity;
        UpdatedAt = updatedAt;
    }

    public void Correct(decimal newQuantity, DateTime updatedAt)
    {
        ValidateQuantity(newQuantity, nameof(newQuantity));
        RequireDate(updatedAt, nameof(updatedAt));

        Quantity = newQuantity;
        UpdatedAt = updatedAt;
    }

    private static void ValidateQuantity(decimal quantity, string parameterName)
    {
        if (quantity < 0)
            throw new ArgumentException("Quantity must be greater than or equal to zero", parameterName);
    }

    private static void ValidatePositiveQuantity(decimal quantity, string parameterName)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", parameterName);
    }

    private static void RequireDate(DateTime value, string parameterName)
    {
        if (value == default)
            throw new ArgumentException($"{parameterName} is required", parameterName);
    }
}

