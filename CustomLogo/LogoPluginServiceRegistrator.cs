using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace CustomLogo
{
    /// <summary>
    /// Registers the logo middleware service.
    /// </summary>
    public class LogoPluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddTransient<Microsoft.AspNetCore.Hosting.IStartupFilter, LogoStartupFilter>();
        }
    }
}
