namespace SQB.Auth.Domain.Models;

public class RefreshTokenResponse
{
    public string Token { get; set; }

    public string RefreshToken { get; set; }
    
    public int Id { get; set; }
    
    public string Email { get; set; }
    
    public string FirstName { get; set; }
    
    public string LastName { get; set; }
}
