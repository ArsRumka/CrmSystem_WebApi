using Identity.Domain.Common;

namespace Identity.Domain.Entities;

public class Organization : Entity
{
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string LicenseKeyHash { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private Organization() : base(Guid.Empty) { }

    public Organization(Guid id, string name, string email, string licenseKeyHash)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name is required");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required");

        if (string.IsNullOrWhiteSpace(licenseKeyHash))
            throw new ArgumentException("License key hash is required");

        Name = name;
        Email = email;
        LicenseKeyHash = licenseKeyHash;
        IsActive = true;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
