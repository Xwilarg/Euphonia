using Euphonia.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Euphonia.API.Controllers;

[ApiController]
[Route("/api/export/")]
public class ExportController : ControllerBase
{

    private readonly ILogger<ExportController> _logger;
    private ExportManager _export;

    public ExportController(ILogger<ExportController> logger, ExportManager export)
    {
        _logger = logger;
        _export = export;
    }


    [HttpPost("prepare")]
    [Authorize]
    public IActionResult GetProgress()
    {
        if (_export.DownloadAllMusic(_logger))
        {
            return StatusCode(StatusCodes.Status204NoContent);
        }
        return StatusCode(StatusCodes.Status403Forbidden);
    }
}
