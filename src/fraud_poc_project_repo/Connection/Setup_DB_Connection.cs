using Amazon.RDS.Util;
using fraud_poc_project_models;
using fraud_poc_project_models.Models.Database;
using fraud_poc_project_repo.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace fraud_poc_project_repo.Connection
{
    public class Setup_DB_Connection : DbContext
    {
        private readonly NpgsqlConnection _dbConnector;
        private readonly Database _databaseOptions;
        private ILogger<Setup_DB_Connection> _logger;
        private readonly IConfiguration _config;

        public NpgsqlConnection DB_Connector { get { return _dbConnector; } }
        public string DB_Schema { get { return _databaseOptions.DBSchema; } }   

        public Setup_DB_Connection(
            ILogger<Setup_DB_Connection> logger,
            DbContextOptions<Setup_DB_Connection> options,
            Database dbOptions,
            IConfiguration config)
            : base(options)
        {
            _logger = logger;
            _config = config;
            _databaseOptions = dbOptions;
            _dbConnector = SetupDatabaseConnection().Build().OpenConnection();
        }

        private NpgsqlDataSourceBuilder SetupDatabaseConnection()
        {
            try
            {
                NpgsqlDataSourceBuilder dsBuilder = new NpgsqlDataSourceBuilder(_config.GetConnectionString("PostgreSQL"));

                if (_databaseOptions.UseRdsToken)
                {
                    dsBuilder.UsePeriodicPasswordProvider(
                        (settings, cancellationToken) =>
                            ValueTask.FromResult(RDSAuthTokenGenerator.GenerateAuthToken(settings.Host, settings.Port, settings.Username)),
                            successRefreshInterval: TimeSpan.FromMinutes(_databaseOptions.SuccessRefreshInterval),
                            failureRefreshInterval: TimeSpan.FromSeconds(_databaseOptions.FailureRefreshInterval));
                }
                return dsBuilder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to Setup Data Base Connection");
                throw;
            }
        }
    }
}
