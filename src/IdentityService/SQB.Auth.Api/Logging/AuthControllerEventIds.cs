namespace SQB.Auth.Logging;

internal static class AuthControllerEventIds
{
    internal static readonly EventId GoogleSignInAttempt = new(1000, "GoogleSignInAttempt");

    internal static readonly EventId InvalidGoogleToken = new(1001, "InvalidGoogleToken");

    internal static readonly EventId GetOrCreateUserFailed = new(1002, "GetOrCreateUserFailed");

    internal static readonly EventId GoogleSignedInSuccess = new(1003, "GoogleSignedInSuccess");

    internal static readonly EventId RegisterAttempt = new(2000, "RegisterAttempt");

    internal static readonly EventId RegisterInvalidNull = new(2001, "RegisterInvalidNull");

    internal static readonly EventId RegisterModelInvalid = new(2002, "RegisterModelInvalid");

    internal static readonly EventId RegisterFailed = new(2003, "RegisterFailed");

    internal static readonly EventId RegisterSuccess = new(2004, "RegisterSuccess");

    internal static readonly EventId LoginAttempt = new(3000, "LoginAttempt");

    internal static readonly EventId LoginInvalidNull = new(3001, "LoginInvalidNull");

    internal static readonly EventId LoginFailed = new(3002, "LoginFailed");

    internal static readonly EventId LoginSuccess = new(3003, "LoginSuccess");

    internal static readonly EventId TokenRefreshAttempt = new(4000, "TokenRefreshAttempt");

    internal static readonly EventId TokenRefreshEmpty = new(4001, "TokenRefreshEmpty");

    internal static readonly EventId TokenRefreshFailed = new(4002, "TokenRefreshFailed");

    internal static readonly EventId TokenRefreshedSuccess = new(4003, "TokenRefreshedSuccess");

    internal static readonly EventId LogoutAttempt = new(5000, "LogoutAttempt");

    internal static readonly EventId LogoutEmptyToken = new(5001, "LogoutEmptyToken");

    internal static readonly EventId LogoutFailed = new(5002, "LogoutFailed");

    internal static readonly EventId LogoutSuccess = new(5003, "LogoutSuccess");

    internal static readonly EventId TokenVerificationAttempt = new(6000, "TokenVerificationAttempt");

    internal static readonly EventId TokenVerificationNoUserId = new(6001, "TokenVerificationNoUserId");

    internal static readonly EventId TokenVerificationFailed = new(6002, "TokenVerificationFailed");

    internal static readonly EventId TokenVerifiedSuccess = new(6003, "TokenVerifiedSuccess");

    internal static readonly EventId TokenVerificationParseFailed = new(6004, "TokenVerificationParseFailed");

    internal static readonly EventId CreateAdminAttempt = new(7000, "CreateAdminAttempt");

    internal static readonly EventId CreateAdminInvalidNull = new(7001, "CreateAdminInvalidNull");

    internal static readonly EventId CreateAdminFailed = new(7002, "CreateAdminFailed");

    internal static readonly EventId CreateAdminSuccess = new(7003, "CreateAdminSuccess");
}
