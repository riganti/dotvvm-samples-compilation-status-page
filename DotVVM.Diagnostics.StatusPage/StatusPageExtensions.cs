using DotVVM.Framework.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;

namespace DotVVM.Diagnostics.StatusPage
{
    public static class StatusPageExtensions
    {
        /// <summary>
        /// Adds Compilation Status Page to the application.
        /// </summary>
        public static IDotvvmServiceCollection AddStatusPage(this IDotvvmServiceCollection services)
        {
            return services.AddStatusPage(StatusPageOptions.CreateDefaultOptions());
        }

        /// <summary>
        /// Adds Compilation Status Page to the application.
        /// </summary>
        public static IDotvvmServiceCollection AddStatusPage(this IDotvvmServiceCollection services, StatusPageOptions options)
        {
            if (options == null)
            {
                options = StatusPageOptions.CreateDefaultOptions();
            }

            services.Services.AddSingleton<StatusPageOptions>(options);
            services.Services.AddTransient<StatusPagePresenter>();
            services.Services.AddTransient<IDotHtmlFilesRuntimePrecompiler, DotHtmlFilesRuntimePrecompiler>();

            services.Services.Configure((DotvvmConfiguration config) =>
            {
                config.RouteTable.Add(options.RouteName, options.Url, "embedded://DotVVM.Diagnostics.StatusPage/Status.dothtml", null, s => s.GetService<StatusPagePresenter>());
            });

            if (options.DelayedPrecompile)
            {
                services.Services.Configure<DotvvmConfiguration>(config=>
                {
                    Task.Factory.StartNew(async () =>
                    {
                        await Task.Delay(options.DelayedPrecompileTimeout * 1000);
                        var precompiler = config.ServiceProvider.GetService<IDotHtmlFilesRuntimePrecompiler>();
                        await precompiler.CompileAll();
                    }, TaskCreationOptions.DenyChildAttach);
                });
            }

            return services;
        }
    }
}
