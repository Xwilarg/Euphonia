using Euphonia.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Euphonia.API.Controllers
{
    [ApiController]
    [Route("/api/")]
    public class RootController : ControllerBase
    {
        private readonly ILogger<DataController> _logger;

        public RootController(ILogger<DataController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "")]
        public Response Get()
        {
            return new()
            {
                Success = true
            };
        }
    }
}
