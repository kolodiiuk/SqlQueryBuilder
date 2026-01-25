namespace SQB.Auth.Application.Options;

public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public double TokenExpirationMinutes { get; set; }

    public double RefreshTokenExpirationDays { get; set; }
}
