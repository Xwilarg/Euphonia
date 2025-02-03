using Euphonia.API.Models;
using Euphonia.API.Models.Request;
using Euphonia.API.Models.Response;
using Euphonia.API.Services;
using Microsoft.AspNetCore.Authorization;
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

    private readonly ILogger<AuthController> _logger;
    private WebsiteManager _manager;

    public AuthController(ILogger<AuthController> logger, WebsiteManager manager)
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

    /// <summary>
    /// Register a path to a local website to be used later on by admins
    /// </summary>
    /// <param name="path">Path to the website</param>
    [HttpPost("register")]
    public IActionResult RegisterEndpoint([FromBody]RegisterData data)
    {
        if (_manager.GetPath(data.Key) != null) // Was endpoint already added
        {
            return StatusCode(StatusCodes.Status200OK, new BaseResponse()
            {
                Success = true,
                Reason = null
            });
        }

        if (!Directory.Exists(data.Path))
        {
            return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
            {
                Success = false,
                Reason = $"Path doesn't exists"
            });
        }
        Program.InitPath(_manager, data.Key, data.Path);
        return StatusCode(StatusCodes.Status200OK, new BaseResponse()
        {
            Success = true,
            Reason = null
        });
    }

    /// <summary>
    /// Authentification and return a session token
    /// </summary>
    /// <param name="password">Admin password</param>
    [HttpPost("token")]
    public IActionResult GetToken([FromBody]string password)
    {
        var hashed = HashPassword(password, "Effy");
        var key = _manager.AdminTokenLookup(hashed);
        if (key == null)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new BaseResponse()
            {
                Success = false,
                Reason = "No admin account match the given password"
            });
        }

        var data = Encoding.UTF8.GetBytes("EffyIsLoveYouButPleaseINeedABetterPassword");
        var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(data);

        var claims = new List<Claim>
        {
            new(ClaimTypes.UserData, key)
        };

        var algorithms = Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature;
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, algorithms);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: credentials);

        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var tokenString = tokenHandler.WriteToken(token);


        return StatusCode(StatusCodes.Status200OK, new TokenResponse()
        {
            Success = true,
            Reason = null,
            Token = tokenString
        });
    }

    /// <summary>
    /// Generate a password hash
    /// </summary>
    /// <param name="password">Password</param>
    [HttpPost("hash")]
    public IActionResult GenerateHash([FromBody]string password)
    {
        var hashed = HashPassword(password, "Effy");

        return StatusCode(StatusCodes.Status200OK, new TokenResponse()
        {
            Success = true,
            Reason = null,
            Token = hashed
        });
    }

    [Authorize]
    [HttpPost("validate")]
    public IActionResult ValidateToken()
    {
        return StatusCode(StatusCodes.Status200OK, new BaseResponse()
        {
            Success = true,
            Reason = null
        });
    }
}
