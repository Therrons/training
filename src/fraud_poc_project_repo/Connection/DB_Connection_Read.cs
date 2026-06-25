using Amazon.RDS.Util;
using fraud_poc_project_models;
using fraud_poc_project_models.Models.Database;
using fraud_poc_project_repo.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace fraud_poc_project_repo.Connection
{
    public class DB_Connection_Read : DbContext, IDB_Connection_Read
    {
        private readonly NpgsqlConnection _connectionRead;
        private readonly Database _databaseOptions;
        private ILogger<DB_Connection_Read> _logger;

        public NpgsqlConnection ReadConn { get { return _connectionRead; } }


        public DB_Connection_Read(
            ILogger<DB_Connection_Read> logger,
            DbContextOptions<DB_Connection_Read>
            options, Database dbOptions)
            : base(options)
        {
            _logger = logger;
            _databaseOptions = dbOptions;
            _connectionRead = SetupDatabaseConnection().Build().OpenConnection();
        }

        /// <summary>
        /// Configure database connection
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="dbOptions"></param>
        /// <returns></returns>
        private NpgsqlDataSourceBuilder SetupDatabaseConnection()
        {
            try
            {
                NpgsqlDataSourceBuilder dsBuilder = new NpgsqlDataSourceBuilder(_databaseOptions.ConnectionStringRead);

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
                _logger.LogError(ex, "Failed to Setup Read Data Base Connection");
                throw;
            }
        }
    }
}
