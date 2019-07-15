using Microsoft.Build.Locator;
using Panacea.Tools.Release.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
            return instances.First().MSBuildPath;
        }

        const string pathToMsBuild = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MsBuild.exe";


        static string GetPath(params string[] parts)
        {
            var arr = new string[]
            {
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            }.Concat(parts);
            return Path.Combine(
                arr.ToArray()
                );
        }
        public static Task Build(LocalProject sinfo, string path = null)
        {
            var msBuildPath = Path.Combine(FindMsBuild(), "msbuild.exe");
            FindMsBuild();
            return Task.Run(() =>
            {
                if (!File.Exists(sinfo.CsProjPath)) return;
                MessageHelper.OnMessage(String.Format("Building {0}...", Path.GetFileName(sinfo.CsProjPath)));
                try
                {
                    var info = new ProcessStartInfo
                    {
                        Arguments =
                            "\"" + sinfo.CsProjPath + "\" /nr:false -fl -flp:logfile="+GetPath("msbuild.txt")+";verbosity=normal /t:Restore,Rebuild /p:Configuration=Release /p:Platform=x86 " + (path != null ? "/p:OutputPath=\"" + path + "\"" : ""),
                        FileName = msBuildPath,

                        //RedirectStandardOutput = true,
                        //RedirectStandardError = true,
                        Verb = "runas"
                    };
                    var process = new Process
                    {
                        StartInfo = info,
                        EnableRaisingEvents = true
                    };

                    process.Start();
                    //process.BeginOutputReadLine();
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
                }
                catch (Exception ex)
                {
                    throw;
                }

            });
        }
    }
}
