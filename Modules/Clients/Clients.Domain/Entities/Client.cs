using Clients.Domain.Enums;

namespace Clients.Domain.Entities;

public class Client
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? MiddleName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public ClientStatus Status { get; private set; }
    public ClientSource Source { get; private set; }
    public bool AllowMarketingEmails { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Client()
    {
    }

    public Client(
        Guid id,
        Guid organizationId,
        string firstName,
        string lastName,
        string? middleName,
        string? email,
        string? phone,
        ClientStatus status,
        ClientSource source,
        bool allowMarketingEmails,
        string? notes,
        DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        ValidateContact(email, phone);
        ValidateStatus(status);
        ValidateSource(source);

        Id = id;
        OrganizationId = organizationId;
        FirstName = Require(firstName, nameof(firstName));
        LastName = Require(lastName, nameof(lastName));
        MiddleName = NormalizeOptional(middleName);
        Email = NormalizeOptional(email);
        Phone = NormalizeOptional(phone);
        Status = status;
        Source = source;
        AllowMarketingEmails = allowMarketingEmails;
        Notes = NormalizeOptional(notes);
        IsActive = true;
        CreatedAt = createdAt;
    }

    public void Update(
        string firstName,
        string lastName,
        string? middleName,
        string? email,
        string? phone,
        ClientStatus status,
        ClientSource source,
        bool allowMarketingEmails,
        string? notes,
        DateTime updatedAt)
    {
        if (updatedAt == default)
            throw new ArgumentException("UpdatedAt is required", nameof(updatedAt));

        ValidateContact(email, phone);
        ValidateStatus(status);
        ValidateSource(source);

        FirstName = Require(firstName, nameof(firstName));
        LastName = Require(lastName, nameof(lastName));
        MiddleName = NormalizeOptional(middleName);
        Email = NormalizeOptional(email);
        Phone = NormalizeOptional(phone);
        Status = status;
        Source = source;
        AllowMarketingEmails = allowMarketingEmails;
        Notes = NormalizeOptional(notes);
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

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void ValidateContact(string? email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Email or phone must be provided");
    }

    private static void ValidateStatus(ClientStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new ArgumentException("Invalid client status", nameof(status));
    }

    private static void ValidateSource(ClientSource source)
    {
        if (!Enum.IsDefined(source))
            throw new ArgumentException("Invalid client source", nameof(source));
    }
}
