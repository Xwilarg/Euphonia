using Euphonia.API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Euphonia.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DataController : ControllerBase
    {
        public string DataPath => Debugger.IsAttached ? "../web/data/" : "../web/data/"; // TODO: move data/ outside of web/

        private readonly ILogger<DataController> _logger;

        public DataController(ILogger<DataController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "ping")]
        public Response GetPing()
        {
            return new()
            {
                Success = true
            };
        }
    }
}
