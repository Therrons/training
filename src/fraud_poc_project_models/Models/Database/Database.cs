namespace fraud_poc_project_models.Models.Database
{
    public class Database
    {
        public bool CreateDatabaseOnStartup { get; set; }
        public string ScriptsFolder { get; set; }
        public bool UseRdsToken { get; set; }
        public string ConnectionStringReadWrite { get; set; }
        public string DBSchema { get; set; }
        public int SuccessRefreshInterval { get; set; }
        public int FailureRefreshInterval { get; set; }
    }
}

