namespace fraud_poc_project_buss.Models.Database
{
    public record Database
    {
        public bool CreateDatabaseOnStartup { get; init; }
        public string ScriptsFolder { get; init; }
        public bool UseRdsToken { get; init; }
        public string ConnectionStringReadWrite { get; init; }
        public string DBSchema { get; init; }
        public int SuccessRefreshInterval { get; init; }
        public int FailureRefreshInterval { get; init; }
    }
}

