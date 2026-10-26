namespace fraud_poc_project_buss.Models.Database
{
    public record Database
    {
        // If true, the app will run the setup postgreSQL scripts when it starts.
        public bool CreateDatabaseOnStartup { get; init; }

        // Folder containing the SQL setup scripts (used when CreateDatabaseOnStartup is true).
        public string ScriptsFolder { get; init; }

        public string ConnectionStringReadWrite { get; init; }

        public string DBSchema { get; init; }
    }
}

