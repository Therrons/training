using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace fraud_poc_project.Controllers
{
    [Route("api/Tester")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> SayHello()
        {
            // Simulate some processing delay
            await Task.Delay(1000);
            return Ok(new { message = "Hello from ValuesController!" });
        }   
    }
}
