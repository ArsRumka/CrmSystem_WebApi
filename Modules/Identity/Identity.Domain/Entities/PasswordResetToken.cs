using Identity.Domain.Common;

namespace Identity.Domain.Entities;

public class PasswordResetToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PasswordResetToken() : base(Guid.Empty) { }

    public PasswordResetToken(Guid id, Guid userId, string tokenHash, DateTime expiresAt, DateTime createdAt)
        : base(id)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required");

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash is required");

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }

    public bool IsExpired(DateTime utcNow)
    {
        return ExpiresAt <= utcNow;
    }

    public void MarkAsUsed(DateTime usedAt)
    {
        if (UsedAt.HasValue)
            throw new InvalidOperationException("Password reset token is already used");

        UsedAt = usedAt;
    }
}
