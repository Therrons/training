using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace docke_web_Api.Controllers
{
    [ApiController]
    [Route("TestFunctions")]
    public class UserInputController: ControllerBase
    {

        [HttpGet("Show_Input")]
        public async Task<ActionResult<string>> Show_Input(string input)
        {
            return $"You entered: {input}";
        }

        [HttpGet("Write_Input")]
        public async Task<ActionResult<string>> Write_Input(string input)
        {
            var _file_Path_Name = Program.file_Path_Name;
            var contentToWrite = (input ?? string.Empty) + Environment.NewLine;

            await System.IO.File.AppendAllTextAsync(_file_Path_Name, contentToWrite);
            return $"Input appended to: {_file_Path_Name}";
        }

        [HttpGet("Print_File_Input")]
        public async Task<ActionResult<string>> Print_File_Input()
        {
            var _file_Path_Name = Program.file_Path_Name;

            if (System.IO.File.Exists(_file_Path_Name))
            {
                var content = await System.IO.File.ReadAllTextAsync(_file_Path_Name);
                return $"File content:{Environment.NewLine}{content}";
            }
            else
            {
                return "File not found.";
            }
        }
    }
}
