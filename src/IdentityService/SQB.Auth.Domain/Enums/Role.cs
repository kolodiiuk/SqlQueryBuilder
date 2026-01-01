namespace SQB.Auth.Domain.Enums;

[Flags]
public enum Role
{
    User = 1,
    Admin = 1 << 1,
}
