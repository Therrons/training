namespace fraud_poc_project_buss.Dto
{
    public enum LoginOutcome
    {
        Succeeded,
        MissingCredentials,
        LockedOut,
        InvalidCredentials
    }

    // The outcome of a login attempt. Token fields are set only when Outcome is Succeeded,
    // and RetryAfterSeconds only when Outcome is LockedOut.
    public sealed class LoginResult
    {
        public LoginOutcome Outcome { get; init; }
        public string? Token { get; init; }
        public DateTime ValidFrom { get; init; }
        public DateTime ValidTo { get; init; }
        public int ExpirationMinutes { get; init; }
        public int RetryAfterSeconds { get; init; }
    }
}
