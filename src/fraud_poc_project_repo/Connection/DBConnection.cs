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

        // Builds the object that knows how to open a database connection.
        private NpgsqlDataSourceBuilder SetupDatabaseConnection()
        {
            try
            {
                return new NpgsqlDataSourceBuilder(_config.GetConnectionString("PostgreSQL"));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to Setup Data Base Connection");
                throw;
            }
        }
    }
}
