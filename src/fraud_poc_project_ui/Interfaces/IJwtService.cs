using System;

namespace fraud_poc_project.Interfaces
{
    public interface IJwtService
    {
        (string token, DateTime validFrom, DateTime validTo)? GenerateToken(string username, string password);

        int ExpirationMinutes { get; }
    }
}
