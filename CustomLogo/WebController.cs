using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace CustomLogo
{
    /// <summary>
    /// Web controller for handling logo uploads and serving custom logos.
    /// Intercepts the original Jellyfin logo paths to serve custom logos.
    /// </summary>
    [ApiController]
    public class WebController : ControllerBase
    {
        private readonly IApplicationPaths _appPaths;
        private readonly string _logoDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebController"/> class.
        /// </summary>
        /// <param name="appPaths">Application paths.</param>
        public WebController(IApplicationPaths appPaths)
        {
            _appPaths = appPaths;
            _logoDirectory = Path.Combine(appPaths.PluginConfigurationsPath, "CustomLogo");

            // Ensure the directory exists
            if (!Directory.Exists(_logoDirectory))
            {
                Directory.CreateDirectory(_logoDirectory);
            }
        }

        /// <summary>
        /// Uploads custom logos.
        /// </summary>
        /// <returns>HTML response.</returns>
        [HttpPost("logo/upload")]
        public async Task<IActionResult> UploadLogo()
        {
            var logo = Request.Form.Files["Logo"];
            var bannerDark = Request.Form.Files["BannerDark"];
            var bannerLight = Request.Form.Files["BannerLight"];

            try
            {
                if (logo != null && logo.Length > 0)
                {
                    var path = Path.Combine(_logoDirectory, "icon-transparent.png");
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await logo.CopyToAsync(stream).ConfigureAwait(false);
                    }
                }

                if (bannerDark != null && bannerDark.Length > 0)
                {
                    var path = Path.Combine(_logoDirectory, "banner-dark.png");
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await bannerDark.CopyToAsync(stream).ConfigureAwait(false);
                    }
                }

                if (bannerLight != null && bannerLight.Length > 0)
                {
                    var path = Path.Combine(_logoDirectory, "banner-light.png");
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await bannerLight.CopyToAsync(stream).ConfigureAwait(false);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Content(
                    "<html><head></head><body>Jellyfin does not have write access to the plugin data directory. Please check the permissions.<br><a href='/web/#/dashboard/plugins'>Return to Jellyfin</a></body></html>",
                    "text/html");
            }
            catch (Exception ex)
            {
                return Content(
                    $"<html><head></head><body>An error occurred: {ex.Message}<br><a href='/web/#/dashboard/plugins'>Return to Jellyfin</a></body></html>",
                    "text/html");
            }

            return Content(
                "<html><head><meta http-equiv='refresh' content='0;url=/web/#/dashboard/plugins' /></head><body>Redirection...</body></html>",
                "text/html");
        }

        // ==================== INTERCEPT ORIGINAL LOGO PATHS ====================
        // Jellyfin Web uses hashed filenames like: icon-transparent.baba78f2a106d9baee83.png
        // We need to intercept these patterns

        /// <summary>
        /// Intercepts requests for icon-transparent.png (with optional hash) and serves custom logo if available.
        /// </summary>
        /// <returns>Custom or original logo.</returns>
        [HttpGet("web/assets/img/icon-transparent.png")]
        public IActionResult GetIconTransparent()
        {
            return ServeLogoOrFallback("icon-transparent.png", "assets/img/icon-transparent.png");
        }

        /// <summary>
        /// Intercepts requests for icon-transparent with hash (e.g., icon-transparent.abc123.png).
        /// </summary>
        /// <returns>Custom or original logo.</returns>
        [HttpGet("web/icon-transparent.{hash}.png")]
        public IActionResult GetIconTransparentHashed(string hash)
        {
            return ServeLogoWithValidatedHash("icon-transparent.png", "icon-transparent", hash);
        }

        /// <summary>
        /// Intercepts requests for banner-dark.png and serves custom banner if available.
        /// </summary>
        /// <returns>Custom or original banner.</returns>
        [HttpGet("web/assets/img/banner-dark.png")]
        public IActionResult GetBannerDarkOriginal()
        {
            return ServeLogoOrFallback("banner-dark.png", "assets/img/banner-dark.png");
        }

        /// <summary>
        /// Intercepts requests for banner-dark with hash.
        /// </summary>
        /// <returns>Custom or original banner.</returns>
        [HttpGet("web/banner-dark.{hash}.png")]
        public IActionResult GetBannerDarkHashed(string hash)
        {
            return ServeLogoWithValidatedHash("banner-dark.png", "banner-dark", hash);
        }

        /// <summary>
        /// Intercepts requests for banner-light.png and serves custom banner if available.
        /// </summary>
        /// <returns>Custom or original banner.</returns>
        [HttpGet("web/assets/img/banner-light.png")]
        public IActionResult GetBannerLightOriginal()
        {
            return ServeLogoOrFallback("banner-light.png", "assets/img/banner-light.png");
        }

        /// <summary>
        /// Intercepts requests for banner-light with hash.
        /// </summary>
        /// <returns>Custom or original banner.</returns>
        [HttpGet("web/banner-light.{hash}.png")]
        public IActionResult GetBannerLightHashed(string hash)
        {
            return ServeLogoWithValidatedHash("banner-light.png", "banner-light", hash);
        }

        // ==================== DIRECT LOGO ENDPOINTS ====================

        /// <summary>
        /// Gets the custom icon directly.
        /// </summary>
        /// <returns>Icon image or not found.</returns>
        [HttpGet("logo/icon")]
        public IActionResult GetIcon()
        {
            var path = Path.Combine(_logoDirectory, "icon-transparent.png");
            if (System.IO.File.Exists(path))
            {
                var bytes = System.IO.File.ReadAllBytes(path);
                return File(bytes, "image/png");
            }

            return NotFound();
        }

        /// <summary>
        /// Gets the custom dark banner directly.
        /// </summary>
        /// <returns>Banner image or not found.</returns>
        [HttpGet("logo/banner-dark")]
        public IActionResult GetBannerDark()
        {
            var path = Path.Combine(_logoDirectory, "banner-dark.png");
            if (System.IO.File.Exists(path))
            {
                var bytes = System.IO.File.ReadAllBytes(path);
                return File(bytes, "image/png");
            }

            return NotFound();
        }

        /// <summary>
        /// Gets the custom light banner directly.
        /// </summary>
        /// <returns>Banner image or not found.</returns>
        [HttpGet("logo/banner-light")]
        public IActionResult GetBannerLight()
        {
            var path = Path.Combine(_logoDirectory, "banner-light.png");
            if (System.IO.File.Exists(path))
            {
                var bytes = System.IO.File.ReadAllBytes(path);
                return File(bytes, "image/png");
            }

            return NotFound();
        }

        /// <summary>
        /// Gets the logo status (which logos are set).
        /// </summary>
        /// <returns>JSON with logo status.</returns>
        [HttpGet("logo/status")]
        public IActionResult GetStatus()
        {
            var status = new
            {
                iconSet = System.IO.File.Exists(Path.Combine(_logoDirectory, "icon-transparent.png")),
                bannerDarkSet = System.IO.File.Exists(Path.Combine(_logoDirectory, "banner-dark.png")),
                bannerLightSet = System.IO.File.Exists(Path.Combine(_logoDirectory, "banner-light.png"))
            };
            return Ok(status);
        }

        /// <summary>
        /// Gets all currently uploaded logos with clickable URLs.
        /// </summary>
        /// <returns>HTML page with logo previews.</returns>
        [HttpGet("logo/list")]
        public IActionResult GetLogos()
        {
            var html = new System.Text.StringBuilder();
            html.Append("<!DOCTYPE html><html><head><title>Custom Logos</title>");
            html.Append("<style>body{font-family:sans-serif;background:#1c1c1e;color:#fff;padding:20px;}");
            html.Append(".logo-item{background:#2a2a2e;padding:20px;margin:10px 0;border-radius:8px;}");
            html.Append(".logo-item h3{margin:0 0 10px 0;}");
            html.Append(".logo-item img{max-width:300px;max-height:150px;background:#000;padding:10px;border-radius:4px;}");
            html.Append(".not-set{color:#888;}</style></head><body>");
            html.Append("<h1>Custom Logo - Uploaded Images</h1>");

            var logoFiles = new[]
            {
                new { Name = "icon-transparent.png", Type = "Icon", Endpoint = "/logo/icon" },
                new { Name = "banner-dark.png", Type = "Dark Banner", Endpoint = "/logo/banner-dark" },
                new { Name = "banner-light.png", Type = "Light Banner", Endpoint = "/logo/banner-light" }
            };

            foreach (var logoFile in logoFiles)
            {
                var path = Path.Combine(_logoDirectory, logoFile.Name);
                html.Append(CultureInfo.InvariantCulture, $"<div class='logo-item'><h3>{logoFile.Type}</h3>");

                if (System.IO.File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    html.Append(CultureInfo.InvariantCulture, $"<img src='{logoFile.Endpoint}?t={DateTime.Now.Ticks}' alt='{logoFile.Type}'/>");
                    html.Append(CultureInfo.InvariantCulture, $"<p>Size: {Math.Round(fileInfo.Length / 1024.0, 2)} KB | ");
                    html.Append(CultureInfo.InvariantCulture, $"Modified: {fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}</p>");
                    html.Append(CultureInfo.InvariantCulture, $"<p>URL: <a href='{logoFile.Endpoint}' style='color:#4fc3f7;'>{logoFile.Endpoint}</a></p>");
                }
                else
                {
                    html.Append("<p class='not-set'>Not set</p>");
                }

                html.Append("</div>");
            }

            html.Append("<p><a href='/web/#/dashboard/plugins' style='color:#4fc3f7;'>← Back to Plugins</a></p>");
            html.Append("</body></html>");

            return Content(html.ToString(), "text/html");
        }

        // ==================== DELETE ENDPOINTS ====================

        /// <summary>
        /// Deletes the custom icon.
        /// </summary>
        /// <returns>OK or not found.</returns>
        [HttpDelete("logo/icon")]
        public IActionResult DeleteIcon()
        {
            var path = Path.Combine(_logoDirectory, "icon-transparent.png");
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                return Ok();
            }

            return NotFound();
        }

        /// <summary>
        /// Deletes the custom dark banner.
        /// </summary>
        /// <returns>OK or not found.</returns>
        [HttpDelete("logo/banner-dark")]
        public IActionResult DeleteBannerDark()
        {
            var path = Path.Combine(_logoDirectory, "banner-dark.png");
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                return Ok();
            }

            return NotFound();
        }

        /// <summary>
        /// Deletes the custom light banner.
        /// </summary>
        /// <returns>OK or not found.</returns>
        [HttpDelete("logo/banner-light")]
        public IActionResult DeleteBannerLight()
        {
            var path = Path.Combine(_logoDirectory, "banner-light.png");
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                return Ok();
            }

            return NotFound();
        }

        // ==================== HELPER METHODS ====================

        private IActionResult ServeLogoOrFallback(string customFileName, string originalRelativePath)
        {
            var customPath = Path.Combine(_logoDirectory, customFileName);

            // If custom logo exists, serve it
            if (System.IO.File.Exists(customPath))
            {
                var bytes = System.IO.File.ReadAllBytes(customPath);
                var fileInfo = new FileInfo(customPath);

                // Set cache control headers to prevent caching and ensure fresh images
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                Response.Headers["Last-Modified"] = fileInfo.LastWriteTimeUtc.ToString("R", CultureInfo.InvariantCulture);

                return File(bytes, "image/png");
            }

            // Otherwise, serve the original from the web path
            var originalPath = Path.Combine(_appPaths.WebPath, originalRelativePath);
            if (System.IO.File.Exists(originalPath))
            {
                var bytes = System.IO.File.ReadAllBytes(originalPath);
                return File(bytes, "image/png");
            }

            return NotFound();
        }

        /// <summary>
        /// Serves logo with validated hash parameter.
        /// Hash is sanitized to only contain alphanumeric characters.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA3003:Review code for file path injection vulnerabilities", Justification = "Hash is sanitized to alphanumeric only, no path traversal possible")]
        private IActionResult ServeLogoWithValidatedHash(string customFileName, string baseName, string hash)
        {
            // Sanitize hash to only contain alphanumeric characters (removes any path traversal attempts)
            if (string.IsNullOrEmpty(hash) || hash.Length > 32)
            {
                return NotFound();
            }

            var sanitizedHash = new System.Text.StringBuilder(hash.Length);
            foreach (char c in hash)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sanitizedHash.Append(c);
                }
            }

            if (sanitizedHash.Length == 0 || sanitizedHash.Length != hash.Length)
            {
                return NotFound();
            }

            var safeHash = sanitizedHash.ToString();
            var customPath = Path.Combine(_logoDirectory, customFileName);

            // If custom logo exists, serve it
            if (System.IO.File.Exists(customPath))
            {
                var bytes = System.IO.File.ReadAllBytes(customPath);
                var fileInfo = new FileInfo(customPath);

                // Set cache control headers to prevent caching and ensure fresh images
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                Response.Headers["Last-Modified"] = fileInfo.LastWriteTimeUtc.ToString("R", CultureInfo.InvariantCulture);

                return File(bytes, "image/png");
            }

            // Otherwise, serve the original from the web path (with sanitized hash)
            var originalFileName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}.png", baseName, safeHash);
            var originalPath = Path.Combine(_appPaths.WebPath, originalFileName);
            if (System.IO.File.Exists(originalPath))
            {
                var bytes = System.IO.File.ReadAllBytes(originalPath);
                return File(bytes, "image/png");
            }

            return NotFound();
        }
    }
}