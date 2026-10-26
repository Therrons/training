namespace fraud_poc_project_buss.Models.Settings
{
    public class ResiliencePipeLineSettings
    {
        public int RetryCount { get; set; }
        public int DelayCount { get; set; }
        public int TimeoutValue { get; set; }
    }
}
