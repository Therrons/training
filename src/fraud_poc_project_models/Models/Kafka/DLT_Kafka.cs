using System.Formats.Asn1;
using static System.Net.Mime.MediaTypeNames;

namespace fraud_poc_project_models.Models.Kafka
{
    public class DLT_Kafka
    {
        public int Id { get; set; }
        public string Topic_Data { get; set; }
        public string Topic_Schema { get; set; }
        public string Topic_Name { get; set; }
        public string Topic_DLT_Name { get; set; }
        public string MessageData { get; set; }
        public string Error { get; set; }
        public DateTime TimeLogged { get; set; }
    }
}
