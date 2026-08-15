using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;
using System.Linq;

namespace fraud_poc_project.Configuration
{
    // Runs the database setup SQL scripts on startup, when Database:CreateDatabaseOnStartup
    // is turned on. Useful for a fresh environment that doesn't have its tables yet.
    public static class DatabaseConfiguration
    {
        public static void ConfigureDatabaseServices(this IServiceCollection services, WebApplicationBuilder builder, IConfiguration configuration)
        {
            Log.Information("CreateDatabaseOnStartup is enabled - initializing database...");

            var scriptsFolder = configuration["Database:ScriptsFolder"] ?? "database";
            var scriptsPath = Path.Combine(builder.Environment.ContentRootPath, scriptsFolder);
            var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");

            if (Directory.Exists(scriptsPath))
            {
                InitializeDatabase(connectionString, scriptsPath, Log.Logger, configuration);
                Log.Information("Database initialization completed.");
            }
            else
            {
                Log.Error("Database scripts folder not found at: {ScriptsPath}", scriptsPath);
            }
        }

        // Runs every .sql file found under scriptsPath, in alphabetical order, against
        // the database. File names are usually prefixed with numbers (01_, 02_, ...) so
        // they run in the right order.
        private static void InitializeDatabase(string connectionString, string scriptsPath, ILogger logger, IConfiguration configuration)
        {
            try
            {
                using var connection = new Npgsql.NpgsqlConnection(connectionString);
                connection.Open();

                logger.Information("Connected to PostgreSQL successfully.");

                var sqlFiles = Directory.GetFiles(scriptsPath, "*.sql", SearchOption.AllDirectories)
                    .OrderBy(f => f)
                    .ToList();

                foreach (var sqlFile in sqlFiles)
                {
                    logger.Information("Executing script: {FileName}", Path.GetFileName(sqlFile));
                    var sql = File.ReadAllText(sqlFile);

                    // insert values into placeholders in the SQL script
                    sql = sql.Replace("${Schema}", configuration["Database:DBSchema"] ?? "fr");
                    sql = sql.Replace("{db_user}", Environment.GetEnvironmentVariable("DB_USERNAME") ?? "public");

                    using var command = new Npgsql.NpgsqlCommand(sql, connection);
                    command.ExecuteNonQuery();

                    logger.Information("Script executed successfully: {FileName}", Path.GetFileName(sqlFile));
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to initialize database.");
                throw;
            }
        }
    }
}
