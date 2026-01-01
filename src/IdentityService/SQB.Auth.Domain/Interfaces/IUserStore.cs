using Microsoft.AspNetCore.Identity;
using SQB.Auth.Domain.Entities;

namespace SQB.Auth.Domain.Interfaces;

public interface ICustomUserStore : IUserStore<User>
{
}
