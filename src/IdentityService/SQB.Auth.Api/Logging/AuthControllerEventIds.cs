namespace SQB.Auth.Logging;

internal static class AuthControllerEventIds
{
    internal static readonly EventId ChangePasswordSuccess = new(1000, nameof(ChangePasswordSuccess));

    internal static readonly EventId ChangePasswordFailure = new(1001, nameof(ChangePasswordFailure));

    internal static readonly EventId ChangePasswordAttempt = new(1002, nameof(ChangePasswordAttempt));

    internal static readonly EventId SignUpAttempt = new(1004, nameof(SignUpAttempt));

    internal static readonly EventId SignUpInvalidNull = new(1005, nameof(SignUpInvalidNull));

    internal static readonly EventId SignUpModelInvalid = new(1006, nameof(SignUpModelInvalid));

    internal static readonly EventId SignUpFailed = new(1007, nameof(SignUpFailed));

    internal static readonly EventId SignUpSuccess = new(1008, nameof(SignUpSuccess));

    internal static readonly EventId SignInAttempt = new(1009, nameof(SignInAttempt));

    internal static readonly EventId SignInInvalidNull = new(1010, nameof(SignInInvalidNull));

    internal static readonly EventId SignInFailed = new(1011, nameof(SignInFailed));

    internal static readonly EventId SignInSuccess = new(1012, nameof(SignInSuccess));

    internal static readonly EventId TokenRefreshAttempt = new(1013, nameof(TokenRefreshAttempt));

    internal static readonly EventId TokenRefreshEmpty = new(1014, nameof(TokenRefreshEmpty));

    internal static readonly EventId TokenRefreshFailed = new(1015, nameof(TokenRefreshFailed));

    internal static readonly EventId TokenRefreshedSuccess = new(1016, nameof(TokenRefreshedSuccess));

    internal static readonly EventId SignOutAttempt = new(1017, nameof(SignOutAttempt));

    internal static readonly EventId SignOutEmptyToken = new(1018, nameof(SignOutEmptyToken));

    internal static readonly EventId SignOutFailed = new(1019, nameof(SignOutFailed));

    internal static readonly EventId SignOutSuccess = new(1020, nameof(SignOutSuccess));

    internal static readonly EventId TokenVerificationAttempt = new(1021, nameof(TokenVerificationAttempt));

    internal static readonly EventId TokenVerificationNoUserId = new(1022, nameof(TokenVerificationNoUserId));

    internal static readonly EventId TokenVerificationFailed = new(1023, nameof(TokenVerificationFailed));

    internal static readonly EventId TokenVerifiedSuccess = new(1024, nameof(TokenVerifiedSuccess));

    internal static readonly EventId TokenVerificationParseFailed = new(1025, nameof(TokenVerificationParseFailed));

    internal static readonly EventId CreateAdminAttempt = new(1026, nameof(CreateAdminAttempt));

    internal static readonly EventId CreateAdminInvalidNull = new(1027, nameof(CreateAdminInvalidNull));

    internal static readonly EventId CreateAdminFailed = new(1028, nameof(CreateAdminFailed));

    internal static readonly EventId CreateAdminSuccess = new(1029, nameof(CreateAdminSuccess));
}
