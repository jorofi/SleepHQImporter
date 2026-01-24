using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uplink.Applications.Websites.CorporateSites.UplinkBg.Services;

public sealed class ShortcutUploadStorage
{
    private readonly string rootPath;
    private readonly ILogger<ShortcutUploadStorage> logger;

    public ShortcutUploadStorage(IConfiguration configuration, ILogger<ShortcutUploadStorage> logger)
    {
        rootPath = configuration["ShortcutUpload:RootPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "shortcut-uploads");
        this.logger = logger;

        this.logger.LogInformation("ShortcutUpload storage configured. RootPath={RootPath}", rootPath);
    }

    public async Task<IReadOnlyList<SavedFile>> SaveAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken)
    {
        if (files is null)
        {
            throw new ArgumentNullException(nameof(files));
        }

        string targetDir = Path.GetFullPath(rootPath);

        Directory.CreateDirectory(targetDir);

        logger.LogInformation("ShortcutUpload saving files. TargetDir={TargetDir}", targetDir);

        var saved = new List<SavedFile>();

        foreach (var file in files.Where(f => f?.Length > 0))
        {
            string ext = Path.GetExtension(file.FileName);
            string savedName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{ext}";
            string fullPath = Path.Combine(targetDir, savedName);

            logger.LogInformation("ShortcutUpload saving file. OriginalName={OriginalName} Length={Length} SavedName={SavedName}", file.FileName, file.Length, savedName);

            await using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            saved.Add(new SavedFile(file.FileName, savedName, fullPath));
        }

        return saved;
    }

    private static string SanitizeFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return string.Empty;
        }

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(folder.Trim().Where(c => !invalid.Contains(c)).ToArray());
        cleaned = cleaned.Replace("..", string.Empty, StringComparison.Ordinal);
        cleaned = cleaned.Replace("\\", string.Empty, StringComparison.Ordinal);
        cleaned = cleaned.Replace("/", string.Empty, StringComparison.Ordinal);
        return cleaned;
    }

    public sealed record SavedFile(string OriginalName, string SavedName, string FullPath);
}
