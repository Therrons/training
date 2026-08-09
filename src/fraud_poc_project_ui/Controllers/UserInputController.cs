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
    [ApiController]
    [Route("TestFunctions")]
    public class UserInputController : ControllerBase
    {
        private readonly IServiceProvider _sp;
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

        #region Testing Docker
        [HttpGet("Write_Input")]
        public async Task<ActionResult<string>> Write_Input(string input)
        {
            var contentToWrite = (input ?? string.Empty) + Environment.NewLine;
            await System.IO.File.AppendAllTextAsync(_filePath, contentToWrite);
            return $"Input appended to: {_filePath}";
        }

        [HttpGet("Print_File_Input")]
        public async Task<ActionResult<string>> Print_File_Input()
        {
            var filePath = _config.GetValue<string>("file_Path_Name");
            if (!System.IO.File.Exists(filePath))
                return "File not found.";

            var content = await System.IO.File.ReadAllTextAsync(_filePath);
            return $"File content:{Environment.NewLine}{content}";
        }
        #endregion


        [HttpGet("Put_Test_In_DB")]
        public async Task<ActionResult<string>> Put_Test_In_DB(string input = "tester")
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

        [HttpGet("Put_Test_In_Kafka")]
        public async Task<ActionResult<string>> Put_Test_In_Kafka(string input = "tester")
        {
            var accNum = (new Random().Next(450000001, 459999999)).ToString();
            var kafkaStream = $"credit-domain-dev-credit-notifier-viya-proxy";
            int iNum = 0;

            try
            {
                //    int itotal = declineReasonsItems.Count;
                //    for (int i = 0; i < itotal; i++)
                //    {
                //        iNum = i;
                //        var producer = _servicesProvider.GetRequiredService<IDomainProducer>();

                //        producer.Produce(kafkaStream, declineReasonsItems[i].Metadata.CorrelationId.ToString(), declineReasonsItems[i]);

                //        _logger.LogInformation($"The following value {Newtonsoft.Json.JsonConvert.SerializeObject(declineReasonsItems[i])}\r\nhas been placed on the '{kafkaStream}' kafka stream");
                //    }

                return new ActionResult<string>("success");
            }
            catch (Exception ex)
            {
                //_logger.LogInformation($"Error: Unable to place {Newtonsoft.Json.JsonConvert.SerializeObject(declineReasonsItems[iNum])}\r\non the '{kafkaStream}' kafka stream due to the following error\r\n{ex}");
                throw;
            }
        }

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
                //_logger.LogError(ex, "Correlation ID: {correlationid} - Failed to write Error to Database for error Model={model}", Guid.NewGuid(), JsonConvert.SerializeObject(input));
                throw;
            }
        }
    }
}
