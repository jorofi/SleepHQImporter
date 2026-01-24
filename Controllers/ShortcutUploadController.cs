using Microsoft.AspNetCore.Mvc;
using SleepHQImporter.Client;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
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

    [HttpPost("upload")]
    [RequestSizeLimit(50L * 1024L * 1024L)]
    public async Task<IActionResult> Upload(
        [FromForm(Name = "file")] IFormFile file,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shortcut upload request started.");

        if (!IsAuthorized())
        {
            _logger.LogWarning("Shortcut upload unauthorized.");
            return Unauthorized();
        }

        if (file is null || file.Length == 0)
        {
            _logger.LogWarning("Shortcut upload rejected: no file received.");
            return BadRequest(new { error = "No file received." });
        }

        if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Shortcut upload rejected: file is not a ZIP archive.");
            return BadRequest(new { error = "File must be a ZIP archive." });
        }

        var meData = await _sleepHQClient.GetV1MeAsync(cancellationToken);
        if (meData is null)
        {
            _logger.LogWarning("SleepHQ /me request failed.");
            return StatusCode(502, new { error = "Failed to retrieve user data from SleepHQ." });
        }

        if (meData.Data.Current_team_id is null)
        {
            _logger.LogWarning("SleepHQ /me response missing Current_team_id.");
            return StatusCode(502, new { error = "User data from SleepHQ is missing team information." });
        }

        var import = await _sleepHQClient.PostV1TeamsTeamIdImportsAsync(
            meData.Data.Current_team_id.Value, 
            null, 
            4678013, //Prisma A20 Device ID
            "Data import iOS Shortcuts",
            cancellationToken);
        
        if (import is null)
        {
            _logger.LogWarning("SleepHQ /teams/{TeamId}/imports request failed.", meData.Data.Current_team_id.Value);
            return StatusCode(502, new { error = "Failed to create import session in SleepHQ." });
        }

        if (import.Data.Id is null)
        {
            _logger.LogWarning("SleepHQ /teams/{TeamId}/imports response missing import ID.", meData.Data.Current_team_id.Value);
            return StatusCode(502, new { error = "Import session data from SleepHQ is missing import ID." });
        }

        // Extract ZIP and upload each file to SleepHQ
        var uploadedFiles = new List<string>();
        using (var zipStream = file.OpenReadStream())
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
        {
            foreach (var entry in archive.Entries)
            {
                // Skip directories (entries with empty names or ending with /)
                if (string.IsNullOrEmpty(entry.Name) || entry.FullName.EndsWith('/'))
                {
                    continue;
                }

                _logger.LogDebug("Processing ZIP entry: {EntryName}, Path: {EntryPath}", entry.Name, entry.FullName);

                using var entryStream = entry.Open();
                using var memoryStream = new MemoryStream();
                await entryStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;

                var contentHash = CalculateContentHash(memoryStream, entry.Name);
                var fileParameter = new FileParameter(memoryStream, entry.Name);

                // Preserve the relative path from the ZIP (use ./ prefix as SleepHQ expects)
                var relativePath = "./" + Path.GetDirectoryName(entry.FullName)?.Replace('\\', '/');
                if (!relativePath.EndsWith('/'))
                {
                    relativePath += "/";
                }

                await _sleepHQClient.PostV1ImportsImportIdFilesAsync(
                    import.Data.Id.Value,
                    entry.Name,
                    relativePath,
                    fileParameter,
                    contentHash,
                    cancellationToken);

                uploadedFiles.Add(entry.FullName);
            }
        }

        _logger.LogInformation("Uploaded {FileCount} files from ZIP to SleepHQ import {ImportId}.", 
            uploadedFiles.Count, import.Data.Id.Value);

        // Trigger file processing
        await _sleepHQClient.PostV1ImportsIdProcessFilesAsync(import.Data.Id.Value, cancellationToken);

        return Ok(new
        {
            importId = import.Data.Id.Value,
            filesUploaded = uploadedFiles.Count,
            files = uploadedFiles
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

    /// <summary>
    /// Calculates the SleepHQ content hash for a file from a stream.
    /// The hash is computed as MD5 of the file content (read as Latin1) concatenated with the lowercase filename, encoded as UTF-8.
    /// </summary>
    /// <param name="stream">The file stream.</param>
    /// <param name="fileName">The file name.</param>
    /// <returns>The MD5 hash as a lowercase hexadecimal string.</returns>
    private static string CalculateContentHash(Stream stream, string fileName)
    {
        using var reader = new StreamReader(stream, Encoding.Latin1, leaveOpen: true);
        var fileText = reader.ReadToEnd();
        var input = fileText + fileName.ToLowerInvariant();
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        stream.Position = 0; // Reset stream position for subsequent reads
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
