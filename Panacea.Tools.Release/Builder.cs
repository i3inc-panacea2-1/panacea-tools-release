using Microsoft.Build.Locator;
using Panacea.Tools.Release.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Panacea.Tools.Release
{
    public static class Builder
    {

        static string FindMsBuild()
        {
            var instances = MSBuildLocator.QueryVisualStudioInstances(VisualStudioInstanceQueryOptions.Default);
            ////if(instances.Any(i=>i.Version))
            return instances.OrderByDescending(d=>d.Version).First().MSBuildPath;
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

        static int RunProcess(string name, string args)
        {
            Debug.WriteLine(name + " " + args);
            var info = new ProcessStartInfo
            {
                Arguments = args,
                FileName = name,

                //RedirectStandardOutput = true,
                //RedirectStandardError = true,
                //Verb = "runas"
            };
            using(var process = new Process
            {
                StartInfo = info,
                EnableRaisingEvents = false
            }){
                try
                {
                    process.Start();
                    process.WaitForExit();
                    return process.ExitCode;
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    Thread.Sleep(1000);
                }
            }
        }
        public static Task Build(LocalProject sinfo, string path = null, string version = null, string fileVersion = null)
        {
            var msBuildPath = Path.Combine(FindMsBuild(), "msbuild.exe");
            FindMsBuild();
            return Task.Run(() =>
            {
                if (!File.Exists(sinfo.CsProjPath)) return;
                MessageHelper.OnMessage(String.Format("Building {0}...", Path.GetFileName(sinfo.CsProjPath)));
               
                var res = RunProcess(
                    msBuildPath,
                    "\"" + sinfo.CsProjPath + "\" /restore /nr:false -fl -flp:logfile=" + GetPath("msbuild2.txt") + ";verbosity=normal /t:Rebuild /p:Configuration=Release;Version=" + version + ";FileVersion=" + fileVersion + " /p:Platform=x86 " + (path != null ? "/p:OutputPath=\"" + path + "\"" : ""));

                if (res == 0)
                {
                    MessageHelper.OnMessage("Build was successful!");
                }
                else
                {
                    throw new Exception("Build failed!");
                }
            });
        }
    }
}
