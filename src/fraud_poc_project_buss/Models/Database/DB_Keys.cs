using System.ComponentModel.DataAnnotations;

namespace fraud_poc_project_buss.Models.Database;

// The username and password used to log in to the database. These normally come from
// a secure secret store (like AWS Secrets Manager) rather than a plain config file.
public record DB_Keys
{
    [Required(ErrorMessage = "Database username cannot be empty")]
    public string username { get; init; } = string.Empty;

    [Required(ErrorMessage = "Database password cannot be empty")]
    public string password { get; init; } = string.Empty;
}
