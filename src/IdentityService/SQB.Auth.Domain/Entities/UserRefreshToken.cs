namespace SQB.Auth.Domain.Entities;

public class UserRefreshToken
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public string Token { get; set; }

    public DateTime Expires { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreatedByIp { get; set; }

    public DateTime? Revoked { get; set; }

    public string? RevokedByIp { get; set; }

    public string? ReplacedByToken { get; set; }

    public bool IsExpired => DateTime.UtcNow >= Expires;

    public bool IsActive => Revoked == null && !IsExpired;

    public User User { get; set; }
}
