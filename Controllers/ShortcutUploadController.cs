using Microsoft.AspNetCore.Mvc;
using SleepHQImporter.Client;
using System.Threading;
using Uplink.Applications.Websites.CorporateSites.UplinkBg.Services;

namespace Uplink.Applications.Websites.CorporateSites.UplinkBg.Controllers;

[ApiController]
[Route("api/shortcuts")]
public sealed class ShortcutUploadController : ControllerBase
{
    private const string ApiKeyHeaderName = "X-Uplink-ApiKey";

    private readonly IConfiguration _configuration;
    private readonly ShortcutUploadStorage _storage;
    private readonly ISleepHQClient _sleepHQClient;
    private readonly ILogger<ShortcutUploadController> _logger;

    public ShortcutUploadController(
        IConfiguration configuration,
        ShortcutUploadStorage storage,
        ISleepHQClient sleepHQClient,
        ILogger<ShortcutUploadController> logger)
    {
        _configuration = configuration;
        _storage = storage;
        _sleepHQClient = sleepHQClient;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        var meData = await _sleepHQClient.GetV1MeAsync(cancellationToken);
        
        return Ok(meData);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(50L * 1024L * 1024L)]
    public async Task<IActionResult> Upload(
        [FromForm(Name = "files")] Microsoft.AspNetCore.Http.IFormFile[] files,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shortcut upload request started. FileCount={FileCount}", files?.Length ?? 0);

        if (!IsAuthorized())
        {
            _logger.LogWarning("Shortcut upload unauthorized.");
            return Unauthorized();
        }

        if (files is null || files.Length == 0)
        {
            _logger.LogWarning("Shortcut upload rejected: no files received.");
            return BadRequest(new { error = "No files received." });
        }

        IReadOnlyList<ShortcutUploadStorage.SavedFile> saved;
        try
        {
            saved = await _storage.SaveAsync(files, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shortcut upload failed while saving files. FileCount={FileCount}", files.Length);
            throw;
        }

        _logger.LogInformation("Shortcut upload completed. SavedCount={SavedCount}", saved.Count);

        return Ok(new
        {
            count = saved.Count,
            files = saved.Select(f => new { originalName = f.OriginalName, savedName = f.SavedName })
        });
    }

    private bool IsAuthorized()
    {
        var expected = _configuration["ShortcutUpload:ApiKey"];

        if (string.IsNullOrWhiteSpace(expected))
        {
            _logger.LogWarning("ShortcutUpload:ApiKey is not configured.");
            return false;
        }

        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var provided))
        {
            return false;
        }

        return string.Equals(expected, provided.ToString(), StringComparison.Ordinal);
    }
}
