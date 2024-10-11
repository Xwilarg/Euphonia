﻿using Euphonia.API.Models;
using Euphonia.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Euphonia.API.Controllers;

[ApiController]
[Route("/api/data/")]
public class DataController : ControllerBase
{

    private readonly ILogger<RootController> _logger;
    private WebsiteManager _manager;

    public DataController(ILogger<RootController> logger, WebsiteManager manager)
    {
        _logger = logger;
        _manager = manager;
    }

    [HttpPost("upload")]
    public Response UploadSong([FromForm]YoutubeForm data)
    {
        return new()
        {
            Success = true,
            Reason = null
        };
    }
}
