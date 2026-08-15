using Npgsql;

namespace fraud_poc_project_repo.Connection
{
    // Gives access to a ready-to-use database connection and the schema name to use with it.
    public interface IDBConnection
    {
        NpgsqlConnection DB_Connector { get; }
        string DB_Schema { get; }
    }
}