using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DotVVM.Diagnostics.StatusPage
{
    public interface IDotHtmlFilesRuntimePrecompiler
    {
        ReadOnlyCollection<DotHtmlFileInfo> Controls { get; }
        ReadOnlyCollection<DotHtmlFileInfo> MasterPages { get; }
        ReadOnlyCollection<DotHtmlFileInfo> Routes { get; }

        void BuildView(DotHtmlFileInfo file);
        Task<bool> CompileAll();
        void ResolveAllFiles();
    }
}