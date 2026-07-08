using fraud_poc_project_models.Models.Database;
using fraud_poc_project_models.Models.Kafka;
using fraud_poc_project_repo.Connection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fraud_poc_project_repo.DB_Operations
{
    public class DB_Operations
    {
        private readonly Setup_DB_Connection _context;
        private readonly ILogger<DB_Operations> _logger;

        public DB_Operations(ILogger<DB_Operations> logger,
            Setup_DB_Connection context)
        {
            _logger = logger;
            _context = context;
        }

        // ==================================
        // CRUD operations
        // ==================================

        public async Task<bool> Capture_Error(DLT_Kafka model)
        {
            var dbConnector = _context.DB_Connector;
            if (dbConnector.State == ConnectionState.Closed) await dbConnector.OpenAsync();

            try
            {
                using var command = new NpgsqlCommand($"CALL {_context.DB_Schema}.sp_insert_dlt_error(@topic_data, @topic_schema, @topic_name, @topic_dlt_name, @message_data, @error);", dbConnector);
                command.Parameters.AddWithValue("@topic_data", model.Topic_Data);
                command.Parameters.AddWithValue("@topic_schema", model.Topic_Schema);
                command.Parameters.AddWithValue("@topic_name", model.Topic_Name);
                command.Parameters.AddWithValue("@topic_dlt_name", model.Topic_DLT_Name);
                command.Parameters.AddWithValue("@message_data", model.MessageData);
                command.Parameters.AddWithValue("@error", model.Error);
                command.ExecuteScalar();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Correlation ID: {correlationid} - Failed to write Error to Database for error Model={model}", Guid.NewGuid(), JsonConvert.SerializeObject(model));
                throw;
            }
        }
    }
}
