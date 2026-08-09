using System.ComponentModel.DataAnnotations;

namespace fraud_poc_project_buss.Models.Database;

public record DB_Keys
{
    [Required(ErrorMessage = "Database username cannot be empty")]
    public string username { get; init; } = string.Empty;

    [Required(ErrorMessage = "Database password cannot be empty")]
    public string password { get; init; } = string.Empty;
}
