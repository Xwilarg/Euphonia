﻿using Euphonia.API.Models;
using Euphonia.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Security.Claims;
using System.Web;

namespace Euphonia.API.Controllers;

[ApiController]
[Route("/api/playlist/")]
public class PlaylistController : ControllerBase
{

    private readonly ILogger<RootController> _logger;
    private HttpClient _client;

    public PlaylistController(ILogger<RootController> logger, HttpClient client)
    {
        _logger = logger;
        _client = client;
    }

    [HttpPost("add")]
    [Authorize]
    public IActionResult ArchiveSong([FromForm] PlaylistForm data)
    {
        var folder = (User.Identity as ClaimsIdentity).FindFirst(x => x.Type == ClaimTypes.UserData).Value;
        var info = Serialization.Deserialize<EuphoniaInfo>(System.IO.File.ReadAllText($"{folder}/info.json"));

        var key = HttpUtility.UrlEncode(data.Name);

        if (info.Playlists.Any(x => x.Key == key))
        {
            return StatusCode(StatusCodes.Status400BadRequest, new Response()
            {
                Success = false,
                Reason = "There is already a playlist with that name"
            });
        }

        if (data.ImageUrl != null)
        {
            Utils.SaveUrlAsImage(_client, data.ImageUrl, $"{folder}icon/playlist/{Utils.CleanPath(data.Name)}.webp");
        }

        info.Playlists.Add(key, new()
        {
            Name = data.FullName ?? data.Name,
            Description = data.Description,
            ImageUrl = $"{Utils.CleanPath(data.Name)}.webp"
        });

        System.IO.File.WriteAllText($"{folder}/info.json", Serialization.Serialize(info));

        return StatusCode(StatusCodes.Status200OK, new Response()
        {
            Success = true,
            Reason = null
        });
    }
}
