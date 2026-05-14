namespace Email.Domain.Entities;

public class EmailTemplate
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Subject { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public bool IsHtml { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    private EmailTemplate()
    {
    }

    public EmailTemplate(
        Guid id,
        Guid organizationId,
        string name,
        string subject,
        string body,
        bool isHtml,
        Guid createdByUserId,
        DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId is required", nameof(createdByUserId));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        Id = id;
        OrganizationId = organizationId;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        IsActive = true;

        SetValues(name, subject, body, isHtml);
    }

    public void Update(
        string name,
        string subject,
        string body,
        bool isHtml,
        bool isActive,
        DateTime updatedAt,
        Guid updatedByUserId)
    {
        if (updatedAt == default)
            throw new ArgumentException("UpdatedAt is required", nameof(updatedAt));

        if (updatedByUserId == Guid.Empty)
            throw new ArgumentException("UpdatedByUserId is required", nameof(updatedByUserId));

        SetValues(name, subject, body, isHtml);
        IsActive = isActive;
        UpdatedAt = updatedAt;
        UpdatedByUserId = updatedByUserId;
    }

    public void Deactivate(DateTime updatedAt, Guid updatedByUserId)
    {
        if (updatedAt == default)
            throw new ArgumentException("UpdatedAt is required", nameof(updatedAt));

        if (updatedByUserId == Guid.Empty)
            throw new ArgumentException("UpdatedByUserId is required", nameof(updatedByUserId));

        IsActive = false;
        UpdatedAt = updatedAt;
        UpdatedByUserId = updatedByUserId;
    }

    private void SetValues(string name, string subject, string body, bool isHtml)
    {
        Name = Require(name, nameof(name), 200);
        Subject = Require(subject, nameof(subject), 300);
        Body = Require(body, nameof(body), 10000);
        IsHtml = isHtml;
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
}
