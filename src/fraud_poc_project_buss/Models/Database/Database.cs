namespace fraud_poc_project_buss.Models.Database
{
    public record Database
    {
        // If true, the app will run the setup postgreSQL scripts when it starts.
        public bool CreateDatabaseOnStartup { get; init; }

        // Folder containing the SQL setup scripts (used when CreateDatabaseOnStartup is true).
        public string ScriptsFolder { get; init; }

        // If true, use a short-lived AWS RDS login token instead of a fixed password.
        public bool UseRdsToken { get; init; }

        public string ConnectionStringReadWrite { get; init; }

        public string DBSchema { get; init; }

        // How often (in minutes) to refresh the RDS login token when it's working fine.
        public int SuccessRefreshInterval { get; init; }

        // How often (in seconds) to retry refreshing the RDS login token after a failure.
        public int FailureRefreshInterval { get; init; }

        public string Host { get; init; }
    }
}

