using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Panacea.Tools.Release.Helpers;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
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
            return Task.Run(() =>
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
            });
        }

        public async Task GetRemoteInfoAsync()
        {
            using (var client = new WebClient())
            {
                try
                {
                    var result = await client.DownloadStringTaskAsync(new Uri(ConfigurationManager.AppSettings["server"] + string.Format("get_module_manifest/{0}/", Name)));
                    RemoteProject = JsonSerializer.DeserializeFromString<RemoteProject>(result);
                }
                catch (WebException ex)
                {
                    var code = (ex.Response as HttpWebResponse).StatusCode;
                    if (code != HttpStatusCode.NotFound) throw;
                }
            }
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
                return RemoteProject?.CommitHash != _repo.Head.Tip.Sha;
            }
        }

        public string CsProjPath { get; }

        public string BasePath { get => Path.GetDirectoryName(CsProjPath); }

        public string FullName { get => Path.GetFileName(CsProjPath).Replace(".csproj", ""); }

        public string Name { get => Path.GetFileName(CsProjPath).Replace(".csproj", "").Split('.').Last(); }

        public ProjectType ProjectType
        {
            get
            {
                if (FullName == "Panacea") return ProjectType.Application;
                var type = FullName.Split('.')[1];
                switch (type)
                {
                    case "Applications":
                        return ProjectType.Application;
                    case "Modules":
                        return ProjectType.Module;
                    case "Tools":
                        return ProjectType.Tool;
                    default: return ProjectType.Library;
                }
            }
        }

        public string GetDirectoryHashCode()
        {
            return Utils.CreateMd5ForFolder(BasePath, new List<string>() { Path.Combine(BasePath, "bin"), Path.Combine(BasePath, "obj") });
        }

        public string CommitHash
        {
            get; private set;
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


    public enum ProjectType
    {
        Application,
        Module,
        Tool,
        Library
    }
}
