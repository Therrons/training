namespace fraud_poc_project_models.Models.Kafka
{
    public class DLQ_Kafka
    {
        public int Id { get; set; }
        public string Topic_Data { get; set; }
        public string Topic_Schema { get; set; }
        public string Topic_Name { get; set; }
        public string Topic_DLQ_Name { get; set; }
        public string MessageData { get; set; }
        public string Error { get; set; }
        public DateTime TimeLogged { get; set; }
    }
}
