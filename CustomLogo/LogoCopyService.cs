using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CustomLogo;

/// <summary>
/// Hosted service that copies custom logos to the web path on server startup.
/// This ensures that static file middleware serves the custom logos instead of the originals.
/// The copy happens on every server start, so it survives Docker container restarts.
/// </summary>
public class LogoCopyService : IHostedService
{
    private readonly ILogger<LogoCopyService> _logger;
    private readonly IApplicationPaths _appPaths;
    private readonly string _logoDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogoCopyService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="appPaths">Application paths.</param>
    public LogoCopyService(ILogger<LogoCopyService> logger, IApplicationPaths appPaths)
    {
        _logger = logger;
        _appPaths = appPaths;
        _logoDirectory = Path.Combine(appPaths.PluginConfigurationsPath, "CustomLogo");
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            CopyLogosToWebPath();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CustomLogo: Failed to copy logos to web path");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Copies custom logos from plugin data directory to the web path.
    /// </summary>
    private void CopyLogosToWebPath()
    {
        if (!Directory.Exists(_logoDirectory))
        {
            _logger.LogInformation("CustomLogo: No custom logo directory found, skipping copy");
            return;
        }

        var webPath = _appPaths.WebPath;
        if (string.IsNullOrEmpty(webPath) || !Directory.Exists(webPath))
        {
            _logger.LogWarning("CustomLogo: Web path not found: {WebPath}", webPath);
            return;
        }

        // Find all icon-transparent files (with hash) in the web directory
        CopyLogoIfExists("icon-transparent.png", webPath);
        CopyLogoIfExists("banner-dark.png", webPath);
        CopyLogoIfExists("banner-light.png", webPath);
    }

    /// <summary>
    /// Copies a custom logo to all matching files in the web directory.
    /// </summary>
    /// <param name="logoFileName">The logo filename (e.g., "icon-transparent.png").</param>
    /// <param name="webPath">The web root path.</param>
    private void CopyLogoIfExists(string logoFileName, string webPath)
    {
        var customLogoPath = Path.Combine(_logoDirectory, logoFileName);
        if (!File.Exists(customLogoPath))
        {
            return;
        }

        var logoBaseName = Path.GetFileNameWithoutExtension(logoFileName);
        var logoExtension = Path.GetExtension(logoFileName);

        // Find all matching files (original and hashed versions)
        // Pattern: icon-transparent.png, icon-transparent.*.png (hashed webpack files)
        var matchingFiles = Directory.GetFiles(webPath, $"{logoBaseName}*{logoExtension}", SearchOption.AllDirectories)
            .Where(f => IsMatchingLogoFile(f, logoBaseName, logoExtension))
            .ToList();

        if (matchingFiles.Count == 0)
        {
            // Also check assets/img subdirectory
            var assetsPath = Path.Combine(webPath, "assets", "img");
            if (Directory.Exists(assetsPath))
            {
                var assetsFiles = Directory.GetFiles(assetsPath, $"{logoBaseName}*{logoExtension}", SearchOption.TopDirectoryOnly)
                    .Where(f => IsMatchingLogoFile(f, logoBaseName, logoExtension))
                    .ToList();
                matchingFiles.AddRange(assetsFiles);
            }
        }

        foreach (var targetFile in matchingFiles)
        {
            try
            {
                File.Copy(customLogoPath, targetFile, overwrite: true);
                _logger.LogInformation("CustomLogo: Copied {Source} to {Target}", logoFileName, targetFile);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "CustomLogo: No write permission for {Target}", targetFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CustomLogo: Failed to copy {Source} to {Target}", logoFileName, targetFile);
            }
        }

        if (matchingFiles.Count == 0)
        {
            _logger.LogDebug("CustomLogo: No matching files found for {Logo} in web path", logoFileName);
        }
    }

    /// <summary>
    /// Checks if a file matches the expected logo pattern.
    /// Matches: icon-transparent.png, icon-transparent.abc123.png (webpack hash)
    /// </summary>
    private static bool IsMatchingLogoFile(string filePath, string baseName, string extension)
    {
        var fileName = Path.GetFileName(filePath);

        // Exact match: icon-transparent.png
        if (fileName.Equals($"{baseName}{extension}", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Hashed match: icon-transparent.abc123def456.png
        // Pattern: baseName + "." + hash + extension
        if (fileName.StartsWith(baseName + ".", StringComparison.OrdinalIgnoreCase) &&
            fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        {
            // Extract the middle part (should be a webpack hash - alphanumeric)
            var middle = fileName.Substring(baseName.Length + 1, fileName.Length - baseName.Length - 1 - extension.Length);
            return !string.IsNullOrEmpty(middle) && middle.All(c => char.IsLetterOrDigit(c));
        }

        return false;
    }
}
