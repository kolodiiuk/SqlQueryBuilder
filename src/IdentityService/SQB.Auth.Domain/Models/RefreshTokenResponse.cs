namespace SQB.Auth.Domain.Models;

public class RefreshTokenResponse
{
    public string Token { get; set; }

    public string RefreshToken { get; set; }

    public Guid Id { get; set; }

    public string Email { get; set; }
}
