using Microsoft.Build.Locator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Tools.Release
{
    public static class Builder
    {
        
        static string FindMsBuild()
        {
            var instances = MSBuildLocator.QueryVisualStudioInstances(VisualStudioInstanceQueryOptions.Default);
            //if(instances.Any(i=>i.Version))
            return null;
        }

        const string pathToMsBuild = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MsBuild.exe";

        public static Task Build(SolutionInfo sinfo)
        {
            FindMsBuild();
            return Task.Run(() =>
            {
                
#if DEBUG
                sinfo.ProjectNames = Directory.GetDirectories(sinfo.PluginsBuildPath).Select(s => s.Remove(0, s.LastIndexOf('\\') + 1)).ToList();
                return true;
#endif
                try
                {
                    if (Directory.Exists(sinfo.CoreBuildPath))
                        Directory.Delete(sinfo.CoreBuildPath, true);
                }
                catch
                {
                    throw new Exception("Could not delete bin folder. The application now will exit");
                }
                MessageHelper.OnMessage(String.Format("Building {0}...", Path.GetFileName(sinfo.SolutionName)));

                var info = new ProcessStartInfo
                {
                    Arguments =
                        "\"" + sinfo.SolutionName + "\" /t:Rebuild /p:Configuration=Release",
                    FileName = pathToMsBuild,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                var process = new Process
                {
                    StartInfo = info,
                    EnableRaisingEvents = true
                };

                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    MessageHelper.OnMessage("Build was successful!");
                    sinfo.ProjectNames = Directory.GetDirectories(sinfo.PluginsBuildPath).Select(s => s.Remove(0, s.LastIndexOf('\\') + 1)).ToList();
                }
                else
                {
                    throw new Exception("Build failed!");
                }
                process.Dispose();
                
            });
        }
    }
}
