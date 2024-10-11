using Euphonia.API.Models;
using Euphonia.API.Services;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Euphonia.API.Controllers;

[ApiController]
[Route("/api/auth/")]
public class AuthController : ControllerBase
{

    private readonly ILogger<RootController> _logger;
    private WebsiteManager _manager;

    public AuthController(ILogger<RootController> logger, WebsiteManager manager)
    {
        _logger = logger;
        _manager = manager;
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
                Success = false,
                Reason = "Path doesn't exists"
            };
        }
        if (!_manager.Endpoints.Contains(path))
        {
            _manager.Endpoints.Add(path);
            _logger.LogInformation($"Adding path {path}");
        }
        return new()
        {
            Success = true,
            Reason = null
        };
    }

    [HttpPost("token")]
    public TokenResponse GetToken([FromBody]string password)
    {
        var hashed = HashPassword(password, "Effy");
        var target = _manager.Endpoints.FirstOrDefault(x =>
        {
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
                Reason = "No admin account match the given password",
                Token = null
            };
        }

        var data = Encoding.UTF8.GetBytes("EffyIsLoveYouButPleaseINeedABetterPassword");
        var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(data);

        var claims = new List<Claim>
        {
            new(ClaimTypes.UserData, target)
        };

        var algorithms = Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature;
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, algorithms);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: credentials);

        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var tokenString = tokenHandler.WriteToken(token);

        return new()
        {
            Success = true,
            Reason = null,
            Token = tokenString
        };
    }

    [HttpPost("hash")]
    public TokenResponse GenerateHash([FromBody]string password)
    {
        var hashed = HashPassword(password, "Effy");
        return new()
        {
            Success = true,
            Reason = null,
            Token = hashed
        };
    }
}
