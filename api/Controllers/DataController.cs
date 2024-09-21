using Euphonia.API.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using System.Text;
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

        private static List<string> _endpoints = new();
        private static Dictionary<string, WebsiteToken> _tokens = new();

        public RootController(ILogger<RootController> logger)
        {
            _logger = logger;
        }

        // Thanks Indra
        private string HashPassword(string password, string salt)
        {
            var saltBytes = Encoding.ASCII.GetBytes(salt);
            var hash = KeyDerivation.Pbkdf2(password, saltBytes, KeyDerivationPrf.HMACSHA512, 210000, 256 / 8);

            return Convert.ToHexString(hash).ToLower();
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
            if (!_endpoints.Contains(path))
            {
                _endpoints.Add(path);
                _logger.LogInformation($"Adding path {path}");
            }
            return new()
            {
                Success = true
            };
        }

        [HttpPost("token")]
        public TokenResponse GetToken([FromBody]string password)
        {
            var hashed = HashPassword(password, "Effy");
            var target = _endpoints.FirstOrDefault(x =>
            {
                var path = Path.Combine(x, "/credentials.json");
                Credentials r;
                try
                {
                    r = JsonSerializer.Deserialize<Credentials>(System.IO.File.ReadAllText(x + "credentials.json"), new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                }
                catch
                {
                    return false;
                }
                return r?.AdminPwd == hashed;
            });
            if (target == null)
            {
                return new()
                {
                    Success = false,
                    Token = null
                };
            }
            var id = Guid.NewGuid();
            _tokens.Add(id.ToString(), new()
            {
                Expiration = DateTime.Now.AddDays(1),
                Path = target
            });
            return new()
            {
                Success = true,
                Token = id.ToString()
            };
        }

        [HttpPost("hash")]
        public TokenResponse GenerateHash([FromBody]string password)
        {
            var hashed = HashPassword(password, "Effy");
            return new()
            {
                Success = true,
                Token = hashed
            };
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
