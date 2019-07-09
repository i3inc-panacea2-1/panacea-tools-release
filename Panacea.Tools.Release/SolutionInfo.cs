using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Tools.Release
{
    public class SolutionInfo
    {
        public string PluginsPath { get; set; }
        public string PluginsBuildPath { get; set; }
        public string CorePath { get; set; }
        public string CoreBuildPath { get; set; }
        public List<string> ProjectNames { get; set; }
        public string SolutionName { get; set; }

    }
}
