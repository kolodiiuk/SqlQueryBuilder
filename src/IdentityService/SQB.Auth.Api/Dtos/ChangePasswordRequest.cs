namespace SQB.Auth.Dtos;

public record ChangePasswordRequest(string OldPassword, string NewPassword);
