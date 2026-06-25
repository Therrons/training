namespace fraud_poc_project_models.Models.Settings
{
    public class PollAppSettings
    {
        public int PollingInterval { get; set; }
        public string Region { get; set; }
        public string ApplicationName { get; set; }
        public string Environment { get; set; }
        public string Profile { get; set; }
        public bool RaiseErrorEvent { get; set; }
    }
}
