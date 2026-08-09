namespace fraud_poc_project_buss.Models.Settings
{
    public record CorsSettings
    {
        public bool Enabled { get; set; } = false;
        public string[] Origins { get; set; }
    }
}
