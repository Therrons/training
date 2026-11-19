using fraud_poc_project_buss.Dto;

namespace fraud_poc_project_buss.Interfaces
{
    public interface ILoginService
    {
        // Checks lockout, validates the credentials, issues a token and records metrics.
        LoginResult Login(string? username, string? password);
    }
}
