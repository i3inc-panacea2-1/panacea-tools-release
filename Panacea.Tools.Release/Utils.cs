using Mono.Cecil;
using Panacea.Tools.Release.Helpers;
using Panacea.Tools.Release.Models;
using PluginPackager2.Models;
using Redmine.Net.Api;
using Redmine.Net.Api.Types;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace Panacea.Tools.Release
{
    public static class Utils
    {

        public static string CreateMd5ForFolder(string path, List<string> foldersToExclude)
        {
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                 .OrderBy(p => p).ToList();
            foreach (var file in files.ToList())
            {
                if (foldersToExclude.Any(x => file.StartsWith(x)))
                {
                    files.Remove(file);
                }
            }
            var md5 = MD5.Create();

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];

                // hash path
                var relativePath = file.Substring(path.Length);
                var pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
                md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                // hash contents
                var contentBytes = File.ReadAllBytes(file);
                if (i == files.Count - 1)
                    md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
                else
                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
            }

            return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
        }

        public static void Panic(string message, params string[] param)
        {
            MessageBox.Show(string.Format(message, param), "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.Shutdown();
            });

        }

        static List<LocalProject> _projects = new List<LocalProject>();

        public static List<LocalProject> Applications { get => _projects.Where(p => p.ProjectType == ProjectType.Application).OrderBy(p => p.Name).ToList(); }

        public static List<LocalProject> Modules { get => _projects.Where(p => p.ProjectType == ProjectType.Module).OrderBy(p => p.Name).ToList(); }

        public static async Task DiscoverSolution()
        {
            MessageHelper.OnMessage("Fetching Git information");
            var repos = await GitHelper.GetAllRepositoriesAsync();

            //DEBUG
            //repos = repos.Take(20).ToList();


            var settings = GitHelper.GetGitSettings();
            foreach (var r in repos.Where(rr => rr.Name.StartsWith("Panacea.Modules")))
            {
                var project = new LocalProject(Path.Combine(settings.RootDir, "Modules", r.Name, "src", r.Name, r.Name + ".csproj"));
                //project.ThrowIfInvalid();
                _projects.Add(project);
                Debug.WriteLine(project.ProjectType);
            }

            foreach (var r in repos.Where(rr => rr.Name.StartsWith("Panacea.Applications")))
            {
                var project = new LocalProject(Path.Combine(settings.RootDir, "Applications", r.Name, "src", r.Name, r.Name + ".csproj"));
                //project.ThrowIfInvalid();
                _projects.Add(project);
                Debug.WriteLine(project.ProjectType);
            }
            var panaceaRepo = repos.First(rr => rr.Name == "Panacea");
            var panacea = new LocalProject(Path.Combine(settings.RootDir, "Applications", panaceaRepo.Name, "src", panaceaRepo.Name, panaceaRepo.Name + ".csproj"));
            Debug.WriteLine(panacea.ProjectType);
            //project.ThrowIfInvalid();
            _projects.Add(panacea);
            MessageHelper.OnMessage("Fetching remote info for: " + panacea.Name);
            await panacea.GetRemoteInfoAsync();
            await panacea.InitializeAsync();

            foreach (var project in _projects.Where(p => p.ProjectType == ProjectType.Module).ToList())
            {
                try
                {
                    MessageHelper.OnMessage("Fetching remote info for: " + project.Name);
                    await project.GetRemoteInfoAsync();
                    await project.InitializeAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    _projects.Remove(project);
                }
            }
            MessageHelper.OnMessage("Analyzing...");
            var allDependencies = _projects
                .SelectMany(p => p.Dependencies)
                .GroupBy(d => d.Name)
                .Select(g => new { Name = g.Key, Versions = g.GroupBy(i => i.Version).OrderByDescending(g1 => System.Version.Parse(g1.Key.Replace("*", "0"))).SelectMany(g1 => g1.Select(a => a.Version)) });
            var duplicates = allDependencies.Where(g => g.Versions.Count() > 1);
            if (duplicates.Any())
            {

                var top = duplicates.Select(d => new Dependency(d.Name, d.Versions.First()));
                var problematic = _projects
                    .Where(p => p.Dependencies.Count > 0)
                    .Select(p =>
                    {
                        return new { Project = p, Problematic = p.Dependencies.Where(d => top.Any(t => t.Name == d.Name && t.Version != d.Version)) };
                    })
                    .Where(p => p.Problematic.Count() > 0);
                if (problematic.Any())
                {
                    var sb = new StringBuilder();
                    foreach (var proj in problematic)
                    {
                        sb.Append(proj.Project.Name);
                        foreach (var dep in proj.Problematic)
                        {
                            sb.Append(Environment.NewLine + "  -" + dep.Name);
                        }
                        sb.Append(Environment.NewLine);
                    }
                    throw new Exception("Projects that do not match Nuget packages: " + Environment.NewLine + sb.ToString());
                }
            }
        }
    }
}
