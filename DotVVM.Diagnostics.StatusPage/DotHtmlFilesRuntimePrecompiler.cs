using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotVVM.Framework.Compilation;
using DotVVM.Framework.Compilation.Parser;
using DotVVM.Framework.Configuration;
using DotVVM.Framework.Controls.Infrastructure;
using DotVVM.Framework.Hosting;
using DotVVM.Framework.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DotVVM.Diagnostics.StatusPage
{
    internal class DotHtmlFilesRuntimePrecompiler : IDotHtmlFilesRuntimePrecompiler
    {
        private static object _locker = new object();
        private static ReadOnlyCollection<DotHtmlFileInfo> routes { get; set; }
        public ReadOnlyCollection<DotHtmlFileInfo> Routes => routes;

        private static ReadOnlyCollection<DotHtmlFileInfo> controls { get; set; }
        public ReadOnlyCollection<DotHtmlFileInfo> Controls => controls;

        private readonly IServiceProvider serviceProvider;
        private readonly DotvvmConfiguration dotvvmConfiguration;
        private readonly StatusPageOptions statusPageOptions;

        public static ConcurrentBag<DotHtmlFileInfo> masterPages { get; set; } = new ConcurrentBag<DotHtmlFileInfo>();
        public ReadOnlyCollection<DotHtmlFileInfo> MasterPages => new ReadOnlyCollection<DotHtmlFileInfo>(masterPages.ToList());

        public DotHtmlFilesRuntimePrecompiler(IServiceProvider serviceProvider, DotvvmConfiguration config, StatusPageOptions statusPageOptions)
        {
            this.serviceProvider = serviceProvider;
            this.dotvvmConfiguration = config;
            this.statusPageOptions = statusPageOptions;
        }
        private readonly Type DotvvmPresenterType = typeof(DotvvmPresenter);
        private bool IsDotvvmPresenter(RouteBase r)
        {
            var presenter = r.GetPresenter(serviceProvider);
            return presenter.GetType().IsAssignableFrom(DotvvmPresenterType);
        }
        public void ResolveAllFiles()
        {
            if (Routes != null) return;
            lock (_locker)
            {
                if (Routes != null) return;

                routes = new ReadOnlyCollection<DotHtmlFileInfo>(dotvvmConfiguration.RouteTable.Select(r => new DotHtmlFileInfo()
                {
                    VirtualPath = r.VirtualPath,
                    Url = r.Url,
                    HasParameters = r.ParameterNames.Any(),
                    DefaultValues = r.DefaultValues.Select(s => s.Key + ":" + s.Value?.ToString()).ToList(),
                    RouteName = r.RouteName,
                    Status = (string.IsNullOrWhiteSpace(r.VirtualPath) || !IsDotvvmPresenter(r))
                        ? CompilationState.NonCompilable
                        : CompilationState.None
                }).ToList());

                controls = new ReadOnlyCollection<DotHtmlFileInfo>(
                    dotvvmConfiguration.Markup.Controls.Where(s => !string.IsNullOrWhiteSpace(s.Src))
                    .Select(s => new DotHtmlFileInfo()
                    {
                        TagName = s.TagName,
                        VirtualPath = s.Src,
                        Namespace = s.Namespace,
                        Assembly = s.Assembly,
                        TagPrefix = s.TagPrefix
                    }).ToList());
            }
        }

        internal void BuildView(DotHtmlFileInfo file, ConcurrentBag<DotHtmlFileInfo> tempList)
        {
            Debug.WriteLine($"Precompiling path: {file?.VirtualPath}");
            if (file.Status != CompilationState.NonCompilable)
            {
                try
                {
                    var controlFactory = serviceProvider.GetRequiredService<IControlBuilderFactory>();

                    var pageBuilder = controlFactory.GetControlBuilder(file.VirtualPath);

                    var compiledControl = pageBuilder.builder.Value.BuildControl(controlFactory, serviceProvider);

                    if (compiledControl is DotvvmView view && view.Directives.TryGetValue(
                            ParserConstants.MasterPageDirective,
                            out var masterPage))
                    {
                        if (MasterPages.All(s => s.VirtualPath != masterPage) &&
                            tempList.All(s => s.VirtualPath != masterPage))
                        {
                            tempList.Add(new DotHtmlFileInfo()
                            {
                                VirtualPath = masterPage
                            });
                        }
                    }

                    file.Status = CompilationState.CompletedSuccessfully;
                    file.Exception = null;
                }
                catch (Exception e)
                {
                    file.Status = CompilationState.CompilationFailed;
                    file.Exception = e.Message;
                }
            }
        }
        /// <summary>
        /// Builds all dothtml file registered in configuration. When the files are already compiled and cached, compilation is not executed.  
        /// </summary>
        public async Task<bool> CompileAll()
        {
            ResolveAllFiles();
            var tempMasterPages = new ConcurrentBag<DotHtmlFileInfo>();
            await BuildPagesAndControls(tempMasterPages);
            await BuildMasterPages(tempMasterPages);
            return MasterPages.Concat(Controls).Concat(Routes).All(s => s.Status != CompilationState.CompilationFailed);
        }

        private async Task<ConcurrentBag<DotHtmlFileInfo>> BuildMasterPages(ConcurrentBag<DotHtmlFileInfo> tempMasterPages)
        {
            while (tempMasterPages.Count > 0)
            {
                tempMasterPages.ToList().ForEach(masterPages.Add);
                masterPages = new ConcurrentBag<DotHtmlFileInfo>(masterPages.Distinct());
                tempMasterPages = new ConcurrentBag<DotHtmlFileInfo>();

                if (statusPageOptions.BuildInParallel)
                {
                    await Task.WhenAll(masterPages.Select(i => Task.Run(() => BuildView(i, tempMasterPages))).ToArray());
                }
                else
                {
                    foreach (var item in masterPages)
                    {
                        BuildView(item, tempMasterPages);
                    }
                }
            }

            return tempMasterPages;
        }

        private async Task BuildPagesAndControls(ConcurrentBag<DotHtmlFileInfo> tempMasterPages)
        {
            if (statusPageOptions.BuildInParallel)
            {
                var compileTasks = Routes.Select(a => Task.Run(() => BuildView(a, tempMasterPages))).ToList();
                compileTasks.AddRange(Controls.Select(a => Task.Run(() => BuildView(a, tempMasterPages))).ToList());
                await Task.WhenAll(compileTasks.ToArray());
            }
            else
            {
                foreach (var item in Routes)
                {
                    BuildView(item, tempMasterPages);
                }
                foreach (var item in Controls)
                {
                    BuildView(item, tempMasterPages);
                }
            }
        }

        /// <summary>
        /// Builds specific dothtml file when it is not cached.
        /// </summary>
        public void BuildView(DotHtmlFileInfo info)
        {
            BuildView(info, masterPages);
        }
    }
}
