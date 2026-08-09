using System.ComponentModel.DataAnnotations;

namespace fraud_poc_project_buss.Models.Settings
{
    public record AppSettings
    {
        [Required(ErrorMessage = "BootstrapServers cannot be emptpy")]
        public string ApplicationName { get; set; }

        [Required(ErrorMessage = "Group Id cannot be emptpy")]
        public string GroupId { get; set; }

        public CorsSettings CORS { get; set; }
    }


}

