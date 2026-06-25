using Amazon.RDS.Util;
using fraud_poc_project_models;
using fraud_poc_project_models.Models.Database;
using fraud_poc_project_repo.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace fraud_poc_project_repo.Connection
{
    public class DB_Connection_Read_Write : DbContext
    {
        private readonly NpgsqlConnection _connectionReadWrite;
        private readonly Database _databaseOptions;
        private ILogger<DB_Connection_Read_Write> _logger;

        public DB_Connection_Read_Write(
            ILogger<DB_Connection_Read_Write> logger,
            DbContextOptions<DB_Connection_Read_Write> options,
            Database dbOptions)
            : base(options)
        {
            _logger = logger;
            _databaseOptions = dbOptions;
            _connectionReadWrite = SetupDatabaseConnection().Build().OpenConnection();
        }

        private NpgsqlDataSourceBuilder SetupDatabaseConnection()
        {
            try
            {
                NpgsqlDataSourceBuilder dsBuilder = new NpgsqlDataSourceBuilder(_databaseOptions.ConnectionStringReadWrite);

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
                _logger.LogError(ex, "Failed to Setup Write Data Base Connection");
                throw;
            }
        }
    }
}
