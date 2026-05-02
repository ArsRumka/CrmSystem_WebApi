using Identity.Domain.Common;
using Identity.Domain.Enums;

namespace Identity.Domain.Entities;

public class OrganizationRequest : Entity
{
    public string CompanyName { get; private set; } = null!;
    public string ContactName { get; private set; } = null!;
    public string ContactEmail { get; private set; } = null!;
    public string ContactPhone { get; private set; } = null!;
    public string? Comment { get; private set; }
    public OrganizationRequestStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public Guid? ProcessedBySystemAdminId { get; private set; }

    private OrganizationRequest() : base(Guid.Empty) { }

    public OrganizationRequest(
        Guid id,
        string companyName,
        string contactName,
        string contactEmail,
        string contactPhone,
        string? comment,
        DateTime createdAt)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("Company name is required");

        if (string.IsNullOrWhiteSpace(contactName))
            throw new ArgumentException("Contact name is required");

        if (string.IsNullOrWhiteSpace(contactEmail))
            throw new ArgumentException("Contact email is required");

        if (string.IsNullOrWhiteSpace(contactPhone))
            throw new ArgumentException("Contact phone is required");

        CompanyName = companyName;
        ContactName = contactName;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        Comment = comment;
        CreatedAt = createdAt;
        Status = OrganizationRequestStatus.Pending;
    }

    public void Approve(Guid systemAdminId, DateTime processedAt)
    {
        EnsurePending();
        EnsureSystemAdmin(systemAdminId);

        Status = OrganizationRequestStatus.Approved;
        ProcessedAt = processedAt;
        ProcessedBySystemAdminId = systemAdminId;
    }

    public void Reject(Guid systemAdminId, DateTime processedAt)
    {
        EnsurePending();
        EnsureSystemAdmin(systemAdminId);

        Status = OrganizationRequestStatus.Rejected;
        ProcessedAt = processedAt;
        ProcessedBySystemAdminId = systemAdminId;
    }

    private void EnsurePending()
    {
        if (Status != OrganizationRequestStatus.Pending)
            throw new InvalidOperationException("Organization request is already processed");
    }

    private static void EnsureSystemAdmin(Guid systemAdminId)
    {
        if (systemAdminId == Guid.Empty)
            throw new ArgumentException("System admin id is required");
    }
}
