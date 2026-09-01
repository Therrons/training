using Amazon.RDS.Util;
using fraud_poc_project_buss.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace fraud_poc_project_repo.Connection
{
    public class DBConnection : DbContext, IDBConnection
    {
        private readonly NpgsqlConnection _dbConnector;
        private readonly Database _databaseOptions;
        private ILogger<DBConnection> _logger;
        private readonly IConfiguration _config;

        public NpgsqlConnection DB_Connector { get { return _dbConnector; } }
        public string DB_Schema { get { return _databaseOptions.DBSchema; } }

        public DBConnection(
            ILogger<DBConnection> logger,
            DbContextOptions<DBConnection> options,
            IOptions<Database> dbOptions,
            IConfiguration config)
            : base(options)
        {
            _logger = logger;
            _config = config;
            _databaseOptions = dbOptions.Value;
            _dbConnector = SetupDatabaseConnection().Build().OpenConnection();
        }

        // Builds the object that knows how to open a database connection. If we're using
        // AWS RDS tokens instead of a plain password, this also sets up automatic
        // refreshing of that token in the background.
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
