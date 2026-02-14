using Euphonia.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Euphonia.API.Controllers;

[ApiController]
[Route("/api/export/")]
public class ExportController : ControllerBase
{

    private readonly ILogger<ExportController> _logger;
    private WebsiteManager _website;
    private ExportManager _export;

    public ExportController(ILogger<ExportController> logger, WebsiteManager website, ExportManager export)
    {
        _logger = logger;
        _website = website;
        _export = export;
    }


    [HttpPost("prepare")]
    [Authorize]
    public IActionResult GetProgress()
    {
        var folder = _website.GetPath((User.Identity as ClaimsIdentity)!.FindFirst(x => x.Type == ClaimTypes.UserData)!.Value)!;

        if (_export.DownloadAllMusic(_logger, folder))
        {
            return StatusCode(StatusCodes.Status204NoContent);
        }
        return StatusCode(StatusCodes.Status403Forbidden);
    }
}
