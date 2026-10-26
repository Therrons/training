namespace fraud_poc_project.Interfaces
{
    public interface IAuthenticationService
    {
        bool ValidateCredentials(string username, string password);
    }
}
