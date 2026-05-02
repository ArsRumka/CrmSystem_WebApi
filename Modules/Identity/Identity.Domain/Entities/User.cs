using Identity.Domain.Common;

namespace Identity.Domain.Entities;

public class User : Entity
{
    public Guid OrganizationId { get; private set; }
    public Guid RoleId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public bool IsEmailConfirmed { get; private set; }

    public Role? Role { get; private set; }

    private User() : base(Guid.Empty) { }

    public User(Guid id, Guid organizationId, Guid roleId, string name, string email, string passwordHash)
        : base(id)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required");

        if (roleId == Guid.Empty)
            throw new ArgumentException("RoleId is required");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required");

        OrganizationId = organizationId;
        RoleId = roleId;
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        IsActive = true;
        IsEmailConfirmed = false;
    }

    public void ChangeRole(Guid newRoleId)
    {
        if (newRoleId == Guid.Empty)
            throw new ArgumentException("Invalid role");

        RoleId = newRoleId;
        Role = null;
    }

    public void ChangePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required");

        PasswordHash = passwordHash;
    }

    public void ConfirmEmail()
    {
        IsEmailConfirmed = true;
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
