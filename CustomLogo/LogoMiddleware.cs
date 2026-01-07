using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CustomLogo
{
    /// <summary>
    /// Middleware that intercepts logo requests before the static file handler.
    /// </summary>
    public class LogoMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _logoDirectory;
        private readonly ILogger<LogoMiddleware> _logger;

        // Match various paths for icon-transparent:
        // /web/icon-transparent.png
        // /web/icon-transparent.hash.png
        // /web/assets/img/icon-transparent.png
        // /web/assets/img/icon-transparent.hash.png
        private static readonly Regex IconPattern = new Regex(
            @"^/web/(assets/img/)?icon-transparent(\.[a-zA-Z0-9]+)?\.png$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Match various paths for banner-dark:
        private static readonly Regex BannerDarkPattern = new Regex(
            @"^/web/(assets/img/)?banner-dark(\.[a-zA-Z0-9]+)?\.png$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Match various paths for banner-light:
        private static readonly Regex BannerLightPattern = new Regex(
            @"^/web/(assets/img/)?banner-light(\.[a-zA-Z0-9]+)?\.png$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="LogoMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware.</param>
        /// <param name="appPaths">Application paths.</param>
        /// <param name="logger">The logger.</param>
        public LogoMiddleware(RequestDelegate next, IApplicationPaths appPaths, ILogger<LogoMiddleware> logger)
        {
            _next = next;
            _logoDirectory = Path.Combine(appPaths.PluginConfigurationsPath, "CustomLogo");
            _logger = logger;
            _logger.LogInformation("CustomLogo Middleware initialized. Logo directory: {LogoDirectory}", _logoDirectory);
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Log all requests for debugging
            if (path.Contains("icon", System.StringComparison.OrdinalIgnoreCase) ||
                path.Contains("banner", System.StringComparison.OrdinalIgnoreCase) ||
                path.Contains("logo", System.StringComparison.OrdinalIgnoreCase) ||
                path.Contains("splash", System.StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("CustomLogo: Request path = {Path}", path);
            }

            // Check if this is a logo request
            string? customLogoPath = null;

            if (IconPattern.IsMatch(path))
            {
                customLogoPath = Path.Combine(_logoDirectory, "icon-transparent.png");
                _logger.LogInformation("CustomLogo: Matched ICON pattern for {Path}", path);
            }
            else if (BannerDarkPattern.IsMatch(path))
            {
                customLogoPath = Path.Combine(_logoDirectory, "banner-dark.png");
                _logger.LogInformation("CustomLogo: Matched BANNER-DARK pattern for {Path}", path);
            }
            else if (BannerLightPattern.IsMatch(path))
            {
                customLogoPath = Path.Combine(_logoDirectory, "banner-light.png");
                _logger.LogInformation("CustomLogo: Matched BANNER-LIGHT pattern for {Path}", path);
            }

            // If we have a custom logo and it exists, serve it
            if (!string.IsNullOrEmpty(customLogoPath))
            {
                _logger.LogInformation(
                    "CustomLogo: Looking for file at {CustomLogoPath}, exists = {Exists}",
                    customLogoPath,
                    File.Exists(customLogoPath));

                if (File.Exists(customLogoPath))
                {
                    context.Response.ContentType = "image/png";
                    // Disable caching to ensure fresh logos are served
                    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    context.Response.Headers["Pragma"] = "no-cache";
                    context.Response.Headers["Expires"] = "0";
                    await context.Response.SendFileAsync(customLogoPath).ConfigureAwait(false);
                    return;
                }
            }

            // Otherwise, continue to the next middleware (static files)
            await _next(context).ConfigureAwait(false);
        }
    }
}