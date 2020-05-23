using System;
using System.Collections.Generic;

namespace DotVVM.Diagnostics.StatusPage
{
    public class DotHtmlFileInfo : IComparable<DotHtmlFileInfo>
    {
        public CompilationState Status { get; internal set; }
        public string Exception { get; internal set; }
        public string TagName { get; internal set; }
        public string Namespace { get; internal set; }
        public string Assembly { get; internal set; }
        public string TagPrefix { get; internal set; }
        public string Url { get; internal set; }

        /// <summary>Gets key of route.</summary>
        public string RouteName { get; internal set; }

        /// <summary>Gets the default values of the optional parameters.</summary>
        public List<string> DefaultValues { get; internal set; }

        /// <summary>Gets or internal sets the virtual path to the view.</summary>
        public string VirtualPath { get; internal set; }

        public bool HasParameters { get; internal set; }

        public int CompareTo(DotHtmlFileInfo other)
        {
            var r = other.VirtualPath.CompareTo(VirtualPath);
            if (r == 0)
            {
                return other.RouteName.CompareTo(RouteName);
            }
            return r;
        }
    }

}
