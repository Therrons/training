using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;
using System.Linq;

namespace fraud_poc_project.Configuration
{
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
                InitializeDatabase(connectionString, scriptsPath, Log.Logger);
                Log.Information("Database initialization completed.");
            }
            else
            {
                Log.Warning("Database scripts folder not found at: {ScriptsPath}", scriptsPath);
            }
        }

        private static void InitializeDatabase(string connectionString, string scriptsPath, Serilog.ILogger logger)
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
