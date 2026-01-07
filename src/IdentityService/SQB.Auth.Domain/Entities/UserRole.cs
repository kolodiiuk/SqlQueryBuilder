namespace SQB.Auth.Domain.Entities;

public class UserRole
{
    public int RoleId { get; set; }
    
    public Guid UserId { get; set; }
}
