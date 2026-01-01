using System.ComponentModel.DataAnnotations;

namespace SQB.Auth.Dtos;

public class RegisterRequest
{
    [Required] public string Email { get; set; }

    [Required] public string Password { get; set; }
}
