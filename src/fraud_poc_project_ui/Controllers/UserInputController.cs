using fraud_poc_project.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        public UserInputController(IServiceProvider sp, IConfiguration config)
        {
            _sp = sp;
            _config = config;
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

        [HttpGet("Get_AWS_Secrets")]
        public async Task<ActionResult<string>> Get_AWS_Secrets()
        {
            try
            {
                var secretName = _config["AWSSecretName"]?.Trim() ?? string.Empty;
                var secretsService = _sp.GetRequiredService<SecretsConfiguration>();
                var secrets = await secretsService.GetSecretAsync(secretName).ConfigureAwait(false);
                return JsonConvert.SerializeObject(secrets);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(ex);
            }
        }
    }
}
