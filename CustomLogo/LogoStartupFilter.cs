using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace CustomLogo
{
    /// <summary>
    /// Startup filter that adds the logo middleware.
    /// </summary>
    public class LogoStartupFilter : IStartupFilter
    {
        /// <inheritdoc />
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                // Add our middleware BEFORE the static files middleware
                app.UseMiddleware<LogoMiddleware>();
                next(app);
            };
        }
    }
}
