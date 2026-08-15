using System.ComponentModel.DataAnnotations;

namespace fraud_poc_project_buss.Models.Kafka
{
    // "DLT" = Dead Letter Topic. When a Kafka message can't be processed (e.g. it's
    // broken, or something went wrong while handling it), we record the details here
    // instead of just losing the message, so someone can investigate later.
    public class DLT_Kafka
    {
        public int Id { get; set; }
        public string Topic_Data { get; set; }
        public string Topic_Schema { get; set; }

        [Required(ErrorMessage = "Topic Name cannot be emptpy")]
        public string Topic_Name { get; set; }

        [Required(ErrorMessage = "Topic DLT Name cannot be emptpy")]
        public string Topic_DLT_Name { get; set; }
        public string MessageData { get; set; }

        [Required(ErrorMessage = "Error Reason for logging to DLT must be specified")]
        public string Error { get; set; }
        public DateTime TimeLogged { get; set; }
    }
}
