using Euphonia.API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text.Json;

namespace Euphonia.API.Controllers
{
    [ApiController]
    [Route("/api/")]
    public class RootController : ControllerBase
    {
        private class WebsiteToken
        {
            public DateTime Expiration { set; get; }
            public string Path { set; get; }
        }

        private readonly ILogger<RootController> _logger;

        private List<string> _endpoints = new();
        private Dictionary<string, WebsiteToken> _tokens = new();

        public RootController(ILogger<RootController> logger)
        {
            _logger = logger;
        }

        [HttpPost("register")]
        public Response RegisterEndpoint([FromBody]string path)
        {
            if (!Path.Exists(path))
            {
                return new()
                {
                    Success = false
                };
            }
            if (!Directory.Exists(path))
            {
                _endpoints.Add(path);
                _logger.LogInformation($"Adding path {path}");
            }
            return new()
            {
                Success = true
            };
        }

        [HttpGet(Name = "token")]
        public TokenResponse GetToken([FromBody]string password)
        {
            var hashed = new Rfc2898DeriveBytes(password, [], 5000);
            var target = _endpoints.FirstOrDefault(x =>
            {
                if (JsonSerializer.Deserialize<Credentials>(Path.Combine(x, "/data/credentials.json"), new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }).AdminPwd)
            });
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
