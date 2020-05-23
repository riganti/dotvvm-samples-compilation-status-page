using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotVVM.Framework.Binding.Properties;
using DotVVM.Framework.Compilation;
using DotVVM.Framework.Compilation.Parser;
using DotVVM.Framework.Configuration;
using DotVVM.Framework.Controls.Infrastructure;
using DotVVM.Framework.Hosting;
using DotVVM.Framework.Routing;
using DotVVM.Framework.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace DotVVM.Diagnostics.StatusPage
{
    public class StatusViewModel : DotvvmViewModelBase
    {
        private readonly StatusPageOptions _statusPageOptions;
        public List<DotHtmlFileInfo> Routes { get; set; }
        public List<DotHtmlFileInfo> MasterPages { get; set; }
        private readonly IDotHtmlFilesRuntimePrecompiler dothtmlPrecompiler;

        public List<DotHtmlFileInfo> Controls { get; set; }
        public string ApplicationPath { get; set; }
        public bool CompileAfterLoad { get; set; }

        public StatusViewModel(StatusPageOptions statusPageOptions,IDotHtmlFilesRuntimePrecompiler precompiler)
        {
            _statusPageOptions = statusPageOptions;
            dothtmlPrecompiler = precompiler;
        }

        public override async Task Init()
        {
            var isAuthorized = await _statusPageOptions.Authorize(Context);
            if (!isAuthorized)
            {
                var response = Context.HttpContext.Response;
                response.StatusCode = 403;

                Context.InterruptRequest();
            }

            if (!Context.IsPostBack)
            {
                MasterPages = new List<DotHtmlFileInfo>();
            }
            ApplicationPath = Context.Configuration.ApplicationPhysicalPath;
            CompileAfterLoad = _statusPageOptions.CompileAfterPageLoads;
            await base.Init();
        }

        public override Task Load()
        {
            if (!Context.IsPostBack)
            {
                dothtmlPrecompiler.ResolveAllFiles();
            }
            return base.Load();
        }

        public override Task PreRender()
        {
            Routes = dothtmlPrecompiler.Routes.OrderByDescending(r => r.Status).ToList();
            MasterPages = dothtmlPrecompiler.MasterPages.OrderByDescending(mp => mp.Status).ToList();
            Controls = dothtmlPrecompiler.Controls.OrderByDescending(c => c.Status).ToList();
            return base.PreRender();
        }

   
        public async Task CompileAll()
        {
            await dothtmlPrecompiler.CompileAll();
        }
        public void BuildView(DotHtmlFileInfo info)
        {
            dothtmlPrecompiler.BuildView(info);
        }
    }
}
