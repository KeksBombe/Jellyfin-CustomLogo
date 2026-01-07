using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using Microsoft.AspNetCore.Http;

namespace CustomLogo
{
    /// <summary>
    /// Middleware that intercepts logo requests before the static file handler.
    /// </summary>
    public class LogoMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _logoDirectory;
        private static readonly Regex IconPattern = new Regex(@"^/web/icon-transparent(\.[a-zA-Z0-9]+)?\.png$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex BannerDarkPattern = new Regex(@"^/web/banner-dark(\.[a-zA-Z0-9]+)?\.png$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex BannerLightPattern = new Regex(@"^/web/banner-light(\.[a-zA-Z0-9]+)?\.png$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="LogoMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware.</param>
        /// <param name="appPaths">Application paths.</param>
        public LogoMiddleware(RequestDelegate next, IApplicationPaths appPaths)
        {
            _next = next;
            _logoDirectory = Path.Combine(appPaths.PluginConfigurationsPath, "CustomLogo");
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Check if this is a logo request
            string? customLogoPath = null;

            if (IconPattern.IsMatch(path))
            {
                customLogoPath = Path.Combine(_logoDirectory, "icon-transparent.png");
            }
            else if (BannerDarkPattern.IsMatch(path))
            {
                customLogoPath = Path.Combine(_logoDirectory, "banner-dark.png");
            }
            else if (BannerLightPattern.IsMatch(path))
            {
                customLogoPath = Path.Combine(_logoDirectory, "banner-light.png");
            }

            // If we have a custom logo and it exists, serve it
            if (!string.IsNullOrEmpty(customLogoPath) && File.Exists(customLogoPath))
            {
                context.Response.ContentType = "image/png";
                // Disable caching to ensure fresh logos are served
                context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
                await context.Response.SendFileAsync(customLogoPath).ConfigureAwait(false);
                return;
            }

            // Otherwise, continue to the next middleware (static files)
            await _next(context).ConfigureAwait(false);
        }
    }
}
