using fraud_poc_project_buss.Models.Kafka;
using fraud_poc_project_repo.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace fraud_poc_project.Controllers
{
    /// <summary>
    /// A grab-bag of manual test endpoints - not part of the real fraud detection
    /// feature. Only registered with the DI container in Debug builds (see
    /// ServiceConfiguration.cs), so these endpoints aren't available in production.
    /// </summary>
    [ApiController]
    [Route("TestFunctions")]
    public class UserInputController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<UserInputController> _logger;
        private readonly IFraudRepository _fraudRepository;
        private readonly string _filePath;

        public UserInputController(IConfiguration config,
            ILogger<UserInputController> logger, IFraudRepository fraudRepository)
        {
            _config = config;
            _logger = logger;
            _fraudRepository = fraudRepository;
            _filePath = _config.GetValue<string>("file_Path_Name") ?? "";

        }

        // Adds a line of text to a test file on disk - just to prove the app can write
        // to its mounted volume when running in Docker.
        [HttpGet("Write_Input")]
        public async Task<ActionResult<string>> Write_Input(string input)
        {
            var contentToWrite = (input ?? string.Empty) + Environment.NewLine;
            await System.IO.File.AppendAllTextAsync(_filePath, contentToWrite);
            return $"Input appended to: {_filePath}";
        }

        // Reads back everything written to that same test file.
        [HttpGet("Print_File_Input")]
        public async Task<ActionResult<string>> Print_File_Input()
        {
            var filePath = _config.GetValue<string>("file_Path_Name");
            if (!System.IO.File.Exists(filePath))
                return "File not found.";

            var content = await System.IO.File.ReadAllTextAsync(_filePath);
            return $"File content:{Environment.NewLine}{content}";
        }

        // Writes a fake error to the database, to prove the database connection works.
        [HttpGet("ValidateDB")]
        public async Task<ActionResult<string>> ValidateDB(string input = "tester")
        {
            try
            {
                await _fraudRepository.CaptureErrorAsync(Guid.NewGuid().ToString(), new DLT_Kafka
                {
                    Error = "Test error message",
                    Topic_Data = "test-topic",
                    MessageData = "test message payload",
                    Topic_DLT_Name = "test dlt name",
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

        /// <summary>
        /// Placeholder endpoint left over from an earlier version of this test controller.
        /// It doesn't actually talk to Kafka right now - it just always returns "success".
        /// </summary>
        [HttpGet("ValidateKafka")]
        public Task<ActionResult<string>> ValidateKafka(string input = "tester")
        {
            return Task.FromResult(new ActionResult<string>("success"));
        }

        // Does the same thing as ValidateDB above (writes a fake error to the database) -
        // kept as a separate endpoint from an earlier round of testing.
        [HttpGet("Retrieve_Test_In_Kafka")]
        public async Task<ActionResult<string>> Retrieve_Test_In_Kafka()
        {
            try
            {
                await _fraudRepository.CaptureErrorAsync(Guid.NewGuid().ToString(), new DLT_Kafka
                {
                    Error = "Test error message",
                    Topic_Data = "test-topic",
                    MessageData = "test message payload",
                    Topic_DLT_Name = "test dlt name",
                    Topic_Name = "topic name test",
                    Topic_Schema = "test topic schema"
                }).ConfigureAwait(false);
                return "Success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write test error to database");
                throw;
            }
        }
    }
}
