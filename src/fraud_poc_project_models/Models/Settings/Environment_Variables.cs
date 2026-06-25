namespace fraud_poc_project_models.Models.Settings
{
    public class Environment_Variables
    {
        public string DBUsername { get; private set; }
        public string DBPassword { get; private set; }
        public string DBHost { get; private set; }
        public string DBPort { get; private set; }
        public string DBName { get; private set; }

        public Environment_Variables Get_Environment_Values(WebApplicationBuilder builder)
        {
            DBUsername = Environment.GetEnvironmentVariable("DB_USERNAME");
            DBPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
            DBHost = Environment.GetEnvironmentVariable("DB_HOST")
                ?? builder.Configuration["Database:Host"]
                ?? "localhost";
            DBPort = Environment.GetEnvironmentVariable("DB_PORT")
                ?? builder.Configuration["Database:Port"]
                ?? "5432";
            DBName = Environment.GetEnvironmentVariable("DB_NAME")
                ?? builder.Configuration["Database:Name"]
                ?? "fraud_db";

            return this;
    }
}}
