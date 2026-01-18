using SQB.Auth.Domain.Entities;
using SQB.Auth.Domain.Models;

namespace SQB.Auth.Dtos;

public class UserDto
{
    public Guid Id { get; set; }

    public string Email { get; set; }

    public string Role { get; set; }

    public static UserDto MapUser(User user)
    {
        var roleName = user.Roles.FirstOrDefault()?.NormalizedName;
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Role = roleName ?? ""
        };
    }
}
