using Microsoft.Build.Locator;
using Panacea.Tools.Release.Models;
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

        public static Task Build(LocalProject sinfo, string path = null)
        {
            FindMsBuild();
            return Task.Run(() =>
            {
                if (!File.Exists(sinfo.CsProjPath)) return;
                MessageHelper.OnMessage(String.Format("Building {0}...", Path.GetFileName(sinfo.CsProjPath)));

                var info = new ProcessStartInfo
                {
                    Arguments =
                        "\"" + sinfo.CsProjPath + "\" /t:Restore,Rebuild /p:Configuration=Release /p:Platform=x86 " + (path != null ? "/p:OutputPath=\"" + path + "\"" : ""),
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
