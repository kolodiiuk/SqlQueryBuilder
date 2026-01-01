using System.ComponentModel.DataAnnotations;

namespace SQB.Auth.Dtos;

public class RefreshTokenRequest
{
    [Required] public string RefreshToken { get; set; }
}
