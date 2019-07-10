﻿using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Panacea.Tools.Release.Helpers;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Panacea.Tools.Release.Models
{
    public class LocalProject
    {
        Repository _repo;

        public LocalProject(string path)
        {
            CsProjPath = path;
        }

        public Task InitializeAsync()
        {
            return Task.Run(async () =>
            {
                _repo = new Repository(new DirectoryInfo(BasePath).Parent.Parent.FullName.ToString(), new RepositoryOptions());

                var settings = GitHelper.GetGitSettings();
                var options = new FetchOptions
                {
                    CredentialsProvider = new CredentialsHandler((url, usernameFromUrl, types) =>
                        new UsernamePasswordCredentials()
                        {
                            Username = settings.Username,
                            Password = RegistrySettings.DecryptData(settings.Password)
                        })
                };
                if (_repo.RetrieveStatus().IsDirty) throw new Exception("Dirty repo: " + FullName);


                var file = Path.Combine(BasePath, "Properties\\AssemblyInfo.cs");
                if (File.Exists(file))
                {
                    var lines = File.ReadLines(file, Encoding.UTF8).ToList();
                    using (var sw = new StreamReader(file))
                    {
                        string version = "0.0.0.0";
                        var assV = new Regex(@"\[assembly: AssemblyVersion\(\""([^\""]*)\""\)\]");
                        var assFV = new Regex(@"\[assembly: AssemblyFileVersion\(\""([^\""]*)\""\)\]");
                        var assIV = new Regex(@"\[assembly: AssemblyInformationalVersion\(\""([^\""]*)\""\)\]");
                        foreach (var line in lines)
                        {

                            var m = assV.Match(line);
                            if (m.Groups.Count == 2 && version == "0.0.0.0")
                            {
                                version = m.Groups[1].Value;
                            }

                            m = assFV.Match(line);
                            if (m.Groups.Count == 2 && string.IsNullOrEmpty(version))
                            {
                                version = m.Groups[1].Value;
                            }

                            m = assIV.Match(line);
                            if (m.Groups.Count == 2)
                            {
                                version = m.Groups[1].Value;
                            }
                        }
                        if (version == "0.0.0.0")
                        {
                            throw new Exception("Version not found: " + Name);
                        }
                        version = version.Replace("*", "0");
                        Version = System.Version.Parse(version);
                        var minimum = new System.Version(2, 50, 0, 0);
                        if (Version < minimum)
                        {
                            SuggestedVersion = minimum;
                        }
                        else
                        {
                            SuggestedVersion = new System.Version(Version.Major, Version.Minor, Version.Build, Version.Revision + 1);
                        }
                    }
                }

                CanBeUpdated = HasDifferentHash && Version != null && (ProjectType == ProjectType.Module || Name == "Panacea");
                await GetDependenciesAsync();
            });
        }

        public bool CanBeUpdated { get; private set; }

        public bool Update { get; set; }

        public async Task GetRemoteInfoAsync()
        {
            using (var client = new WebClient())
            {
                try
                {
                    string name = Name;
                    if (Name == "Panacea") name = "core";
                    var result = await client.DownloadStringTaskAsync(new Uri(ConfigurationManager.AppSettings["server"] + string.Format("get_module_manifest/{0}/", name)));
                    RemoteProject = JsonSerializer.DeserializeFromString<RemoteProject>(result);
                }
                catch (WebException ex)
                {
                    var code = (ex.Response as HttpWebResponse).StatusCode;
                    if (code != HttpStatusCode.NotFound) throw;
                }
            }
        }

        public List<Dependency> Dependencies { get; private set; } = new List<Dependency>();

        public Task GetDependenciesAsync()
        {
            return Task.Run(async () =>
            {
                if (!File.Exists(CsProjPath)) return;
                var regex = new Regex(@"<PackageReference Include=\""([^\""]*)\"">[^<Version>]*<Version>([^<\/Version>]*)");
                using (var reader = new StreamReader(CsProjPath))
                {
                    var text = await reader.ReadToEndAsync();
                    var matches = regex.Matches(text);
                    foreach (Match match in matches)
                    {
                        Debug.WriteLine(match.Groups[2].Value);
                        Dependencies.Add(new Dependency(match.Groups[1].Value, System.Version.Parse(match.Groups[2].Value.Split('-').First()).Major.ToString() + ".*.*"));
                    }

                }
            });
        }

        public System.Version Version { get; private set; }

        public System.Version SuggestedVersion
        {
            get; set;
        }

        public RemoteProject RemoteProject { get; private set; }

        public bool IsValid()
        {
            return File.Exists(CsProjPath);
        }

        public bool HasDifferentHash
        {
            get
            {
                return RemoteProject?.CommitHash != CommitHash;
            }
        }

        public string CommitHash { get => _repo.Head.Tip.Sha; }

        public string CsProjPath { get; }

        public string BasePath { get => Path.GetDirectoryName(CsProjPath); }

        public string FullName { get => Path.GetFileName(CsProjPath).Replace(".csproj", ""); }

        public string Name { get => Path.GetFileName(CsProjPath).Replace(".csproj", "").Split('.').Last(); }

        ProjectType _type = ProjectType.Unknown;
        public ProjectType ProjectType
        {
            get
            {
                if (_type != ProjectType.Unknown) return _type;
                if (FullName == "Panacea")
                {
                    return _type = ProjectType.Application;
                }

                var type = FullName.Split('.')[1];
                switch (type)
                {
                    case "Applications":
                        return _type = ProjectType.Application;
                    case "Modules":
                        return _type = ProjectType.Module;
                    case "Tools":
                        return ProjectType.Tool;
                    default: return _type = ProjectType.Library;
                }
            }
        }

        public string GetDirectoryHashCode()
        {
            return Utils.CreateMd5ForFolder(BasePath, new List<string>() { Path.Combine(BasePath, "bin"), Path.Combine(BasePath, "obj") });
        }


        public Task SetAssemblyVersion(string version)
        {
            return Task.Run(() =>
            {
                var file = Path.Combine(BasePath, "Properties\\AssemblyInfo.cs");
                if (File.Exists(file))
                {
                    var lines = File.ReadLines(file, Encoding.UTF8).ToList();
                    var FileVersionAdded = false;
                    using (var sw = new StreamWriter(File.Open(file, FileMode.OpenOrCreate)))
                    {
                        foreach (var line in lines)
                        {
                            if (line.Contains("AssemblyVersion") && !line.StartsWith("//"))
                            {
                                sw.Write("[assembly: AssemblyVersion(\"" + version + "\")]" + Environment.NewLine);
                            }
                            else if (line.Contains("AssemblyFileVersion") && !line.StartsWith("//"))
                            {
                                FileVersionAdded = true;
                                sw.Write("[assembly: AssemblyFileVersion(\"" + version + "\")]" + Environment.NewLine);
                            }
                            else
                            {
                                sw.Write(line + Environment.NewLine);
                            }
                        }
                        if (!FileVersionAdded)
                            sw.Write("[assembly: AssemblyFileVersion(\"" + version + "\")]" + Environment.NewLine);

                        //to vs exei bug mi to vgaleis
                        sw.Write(Environment.NewLine + Environment.NewLine);
                    }
                }
            });
        }
    }

    public class Dependency
    {
        public string Version { get; }

        public string Name { get; }
        public Dependency(string name, string version)
        {
            Name = name;
            Version = version;
        }
    }

    public enum ProjectType
    {
        Application,
        Module,
        Tool,
        Library,
        Unknown
    }
}