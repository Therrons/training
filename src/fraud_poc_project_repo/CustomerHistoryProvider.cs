using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Interfaces;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace fraud_poc_project_repo
{
    // Reads a customer's previously evaluated transactions from fraud_event.
    public class CustomerHistoryProvider : ICustomerHistoryProvider
    {
        private readonly string _connectionString;
        private readonly string _schema;
        private readonly ILogger<CustomerHistoryProvider> _logger;

        public CustomerHistoryProvider(string connectionString, string schema, ILogger<CustomerHistoryProvider> logger)
        {
            _connectionString = connectionString;
            _schema = schema;
            _logger = logger;
        }

        public async Task<CustomerHistory> GetHistoryAsync(
            string customerId,
            Guid transactionId,
            DateTime transactionTime,
            DateTime velocityFrom,
            DateTime lookbackFrom)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync().ConfigureAwait(false);

                // One round trip: counts both windows and the average amount. The current transaction is excluded.
                await using var cmd = new NpgsqlCommand(
                    $"SELECT " +
                    $"COUNT(*) FILTER (WHERE transaction_time >= @velocity_from) AS recent_count, " +
                    $"COUNT(*) AS baseline_count, " +
                    $"COALESCE(AVG(ABS(amount)), 0) AS average_amount " +
                    $"FROM \"{_schema}\".fraud_event " +
                    $"WHERE customer_id = @customer_id " +
                    $"AND transaction_id <> @transaction_id " +
                    $"AND transaction_time < @transaction_time " +
                    $"AND transaction_time >= @lookback_from", conn);

                cmd.Parameters.Add("customer_id", NpgsqlDbType.Varchar).Value = customerId;
                cmd.Parameters.Add("transaction_id", NpgsqlDbType.Uuid).Value = transactionId;
                cmd.Parameters.Add("transaction_time", NpgsqlDbType.TimestampTz).Value = transactionTime;
                cmd.Parameters.Add("velocity_from", NpgsqlDbType.TimestampTz).Value = velocityFrom;
                cmd.Parameters.Add("lookback_from", NpgsqlDbType.TimestampTz).Value = lookbackFrom;

                await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                if (!await reader.ReadAsync().ConfigureAwait(false))
                {
                    return new CustomerHistory(0, 0, 0m);
                }

                // COUNT returns bigint in Postgres, so read it as Int64 and narrow it
                return new CustomerHistory(
                    (int)reader.GetInt64(0),
                    (int)reader.GetInt64(1),
                    reader.GetDecimal(2));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load customer history for customer={CustomerId}", customerId);
                throw;
            }
        }
    }
}
