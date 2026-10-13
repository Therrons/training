using Microsoft.AspNetCore.Builder;
using System;

namespace fraud_poc_project.Settings
{
    // Reads database connection details as well as kafka settings from environment
    // variables (used when running in Docker/Kubernetes), falling back to values from appsettings.json
    public class Environment_Variables
    {
        public string DBUsername { get; private set; }
        public string DBPassword { get; private set; }
        public string DBHost { get; private set; }
        public string DBPort { get; private set; }
        public string DBName { get; private set; }
        public string KAFKAUSER { get; private set; }
        public string KAFKAPASSWORD { get; private set; }
        public string APIUSERNAME { get; private set; }
        public string APIPASSWORD { get; private set; }

        public Environment_Variables Get_Environment_Values(WebApplicationBuilder builder)
        {
            var envName = builder.Environment.EnvironmentName.Trim().ToLower();

            if (envName == "loc") // secrets are obtained from secrets.json when running locally
            {
                var configuration = builder.Configuration;

                DBUsername = configuration["db_username"] ?? "";
                DBPassword = configuration["db_password"] ?? "";
                KAFKAUSER = configuration["kafka_user"] ?? "";
                KAFKAPASSWORD = configuration["kafka_password"] ?? "";
                APIUSERNAME = configuration["api_username"] ?? "";
                APIPASSWORD = configuration["api_password"] ?? "";

                Environment.SetEnvironmentVariable("DB_USERNAME", DBUsername);
                Environment.SetEnvironmentVariable("DB_PASSWORD", DBPassword);
                Environment.SetEnvironmentVariable("KAFKA_USER", KAFKAUSER);
                Environment.SetEnvironmentVariable("KAFKA_PASSWORD", KAFKAPASSWORD);
                Environment.SetEnvironmentVariable("API_USERNAME", APIUSERNAME);
                Environment.SetEnvironmentVariable("API_PASSWORD", APIPASSWORD);
            }

            else // secrets are obtained from github secrets and variables
            {
                DBUsername = Environment.GetEnvironmentVariable("DB_USERNAME");
                DBPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
                KAFKAUSER = Environment.GetEnvironmentVariable("KAFKA_USER");
                KAFKAPASSWORD = Environment.GetEnvironmentVariable("KAFKA_PASSWORD");
                APIUSERNAME = Environment.GetEnvironmentVariable("API_USERNAME");
                APIPASSWORD = Environment.GetEnvironmentVariable("API_PASSWORD");
            }

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
    }
}
