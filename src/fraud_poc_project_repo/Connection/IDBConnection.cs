using Npgsql;

namespace fraud_poc_project_repo.Connection
{
    public interface IDBConnection
    {
        NpgsqlConnection DB_Connector { get; }
        string DB_Schema { get; }
    }
}