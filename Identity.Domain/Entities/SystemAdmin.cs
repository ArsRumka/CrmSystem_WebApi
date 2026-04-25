using Identity.Domain.Common;

namespace Identity.Domain.Entities;

public class SystemAdmin : Entity
{
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SystemAdmin() : base(Guid.Empty) { }

    public SystemAdmin(Guid id, string name, string email, string passwordHash, DateTime createdAt)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("System admin name is required");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required");

        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
        IsActive = true;
    }

    public void ChangePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required");

        PasswordHash = passwordHash;
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
