namespace fraud_poc_project_buss.Models.Database
{
    // Settings that control how the app connects to and sets up its database.
    // These are loaded from the "Database" section of appsettings.json.
    public record Database
    {
        // If true, the app will run the setup SQL scripts when it starts.
        public bool CreateDatabaseOnStartup { get; init; }

        // Folder containing the SQL setup scripts (used when CreateDatabaseOnStartup is true).
        public string ScriptsFolder { get; init; }

        // If true, use a short-lived AWS RDS login token instead of a fixed password.
        public bool UseRdsToken { get; init; }

        public string ConnectionStringReadWrite { get; init; }

        // Which database schema (like a named folder of tables) the app should use.
        public string DBSchema { get; init; }

        // How often (in minutes) to refresh the RDS login token when it's working fine.
        public int SuccessRefreshInterval { get; init; }

        // How often (in seconds) to retry refreshing the RDS login token after a failure.
        public int FailureRefreshInterval { get; init; }
    }
}

