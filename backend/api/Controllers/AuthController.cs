using Euphonia.API.Models.Request;
using Euphonia.API.Models.Response;
using Euphonia.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Euphonia.API.Controllers;

[ApiController]
[Route("/api/auth/")]
public class AuthController : ControllerBase
{

    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
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

    /// <summary>
    /// Authentification and return a session token
    /// </summary>
    /// <param name="password">Admin password</param>
    [HttpPost("token")]
    public IActionResult GetToken([FromBody]string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return StatusCode(StatusCodes.Status400BadRequest);

        var hashed = HashPassword(password, "Effy");

        if (!System.IO.File.Exists("/data/credentials.json")) return StatusCode(StatusCodes.Status401Unauthorized);

        if (Serialization.Deserialize<EuphoniaCredentials>(System.IO.File.ReadAllText("/data/credentials.json"))?.AdminPwd != hashed)
            return StatusCode(StatusCodes.Status401Unauthorized);

        var data = Encoding.UTF8.GetBytes("EffyIsLoveYouButPleaseINeedABetterPassword");
        var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(data);

        var algorithms = Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature;
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, algorithms);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            //claims: claims,
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
