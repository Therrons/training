using System.ComponentModel.DataAnnotations;

namespace fraud_poc_project_buss.Models.Settings
{
    public record AppSettings
    {
        [Required(ErrorMessage = "ApplicationName cannot be empty")]
        public string ApplicationName { get; set; }

        [Required(ErrorMessage = "GroupId cannot be empty")]
        public string GroupId { get; set; }

        public CorsSettings CORS { get; set; }
    }
}

