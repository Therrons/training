using System.ComponentModel.DataAnnotations;

namespace fraud_poc_project_buss.Models.Settings
{
    // General application-wide settings, loaded from the "AppSettings" section of
    // appsettings.json.
    public record AppSettings
    {
        [Required(ErrorMessage = "ApplicationName cannot be empty")]
        public string ApplicationName { get; set; }

        // Kafka consumer group id - consumers in the same group share the work of
        // reading messages from a topic.
        [Required(ErrorMessage = "GroupId cannot be empty")]
        public string GroupId { get; set; }

        public CorsSettings CORS { get; set; }
    }
}

