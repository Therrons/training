using Npgsql;

namespace fraud_poc_project_repo.Connection
{
    public interface IDBConnection
    {
        NpgsqlConnection DbConnector { get; }
        string DbSchema { get; }
    }
}