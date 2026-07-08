using fraud_poc_project.Configuration;
using fraud_poc_project_models.Models.Kafka;
using fraud_poc_project_repo.Connection;
using fraud_poc_project_repo.DB_Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Data;
using System.Threading.Tasks;

namespace fraud_poc_project.Controllers
{
    [ApiController]
    [Route("TestFunctions")]
    public class UserInputController : ControllerBase
    {
        private readonly IServiceProvider _sp;
        private readonly IConfiguration _config;
        private readonly ILogger<UserInputController> _logger;
        private readonly DB_Operations _dbOps;

        public UserInputController(IConfiguration config,
            ILogger<UserInputController> logger, DB_Operations dbOps)
        {
            _config = config;
            _logger = logger;
            _dbOps = dbOps;
        }

        [HttpGet("Show_Input")]
        public ActionResult<string> Show_Input(string input)
        {
            return $"You entered: {input}";
        }

        [HttpGet("Write_Input")]
        public async Task<ActionResult<string>> Write_Input(string input)
        {
            var contentToWrite = (input ?? string.Empty) + Environment.NewLine;
            await System.IO.File.AppendAllTextAsync(Program.file_Path_Name, contentToWrite);
            return $"Input appended to: {Program.file_Path_Name}";
        }

        [HttpGet("Print_File_Input")]
        public async Task<ActionResult<string>> Print_File_Input()
        {
            if (!System.IO.File.Exists(Program.file_Path_Name))
                return "File not found.";

            var content = await System.IO.File.ReadAllTextAsync(Program.file_Path_Name);
            return $"File content:{Environment.NewLine}{content}";
        }

        [HttpGet("Put_Test_In_DB")]
        public async Task<ActionResult<string>> Put_Test_In_DB(string input = "tester")
        {
            try
            {
                await _dbOps.Capture_Error(new DLT_Kafka {
                    Error = "Test error message",
                    Topic_Data = "test-topic",
                    MessageData = "test message payload",
                    Topic_DLT_Name =  "test dlt name",
                    Topic_Name = "topic name test",
                    Topic_Schema = "test topic schema"
                }).ConfigureAwait(false);   
                return "Success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Correlation ID: {correlationid} - Failed to write Error to Database for error Model={model}", Guid.NewGuid(), JsonConvert.SerializeObject(input));
                throw;
            }
        }
    }
}
