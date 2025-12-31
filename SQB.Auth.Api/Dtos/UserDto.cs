using SQB.Auth.Domain.Models;
using SQB.Auth.Models;

namespace SQB.Auth.Dtos;

public class UserDto
{
    public int Id { get; set; }

    public string Email { get; set; }

    public static UserDto MapUser(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
        };
    }
}
