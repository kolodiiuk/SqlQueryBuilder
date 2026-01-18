using System.Text.RegularExpressions;

namespace SQB.Auth.Dtos;

public partial class SignInRequest
{
    public string Email { get; set; }

    public string Password { get; set; }

    public bool IsValid()
    {
        return Email != null
               && Password != null
               && EmailRegex().IsMatch(Email)
               && PasswordRegex().IsMatch(Password);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$")]
    private static partial Regex PasswordRegex();
}
