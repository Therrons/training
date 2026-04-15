using System.ComponentModel.DataAnnotations;

namespace docke_web_Api_models;

public record DB_Keys
{
    [Required(ErrorMessage = "Database Key cannot be empty")]
    public string id { get; init; } // Changed to 'init'

    [Required(ErrorMessage = "Database Key Value cannot be empty")]
    public string secret { get; init; } // Changed to 'init'
}