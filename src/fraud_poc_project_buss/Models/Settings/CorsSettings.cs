namespace fraud_poc_project_buss.Models.Settings
{
    // Controls CORS (Cross-Origin Resource Sharing) - which other websites are allowed
    // to call this API directly from a browser.
    public record CorsSettings
    {
        public bool Enabled { get; set; } = false;

        // The list of allowed website addresses, e.g. "https://myapp.com".
        public string[] Origins { get; set; }
    }
}
