using DotVVM.Framework.Hosting;
using System;
using System.Threading.Tasks;

namespace DotVVM.Diagnostics.StatusPage
{
    public class StatusPageOptions
    {
        /// <summary>
        /// Default route name the status page.
        /// </summary>
        public string RouteName { get; set; } = "StatusPage";

        /// <summary>
        /// Default url of the status page.
        /// </summary>
        public string Url { get; set; } = "_diagnostics/status";
        /// <summary>
        /// Compile all pages at first access of the status page.
        /// </summary>
        public bool CompileAfterPageLoads { get; set; } = true;

        /// <summary>
        /// Sets whether all views should be pre-compiled at startup time or not. 
        /// </summary>
        public bool DelayedPrecompile { get; set; } = true;

        /// <summary>
        /// Pre-compilation is performed after specified timeout (seconds). Default value is 1 minute.
        /// </summary>
        public int DelayedPrecompileTimeout { get; set; } = 60;
        /// <summary>
        /// Sets whether pre-compilation should use only one core or more. 
        /// </summary>
        public bool BuildInParallel { get; set; } = true;
        /// <summary>
        /// This method authorizes user to access status page. Status Page is accessible only from localhost by default.
        /// </summary>
        public Func<IDotvvmRequestContext, Task<bool>> Authorize { get; set; }
            = context => Task.FromResult(context.HttpContext.Request.Url.IsLoopback);
        
        public static StatusPageOptions CreateDefaultOptions()
        {
            return new StatusPageOptions();
        }
    }
}
