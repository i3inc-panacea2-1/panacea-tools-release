using Ionic.Zip;
using LibGit2Sharp;
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



                var minimum = new System.Version(2, 50, 0, 0);
                if (RemoteProject == null || RemoteProject.Version == null)
                {
                    SuggestedVersion = minimum;
                }
                else
                {
                    var v = System.Version.Parse(RemoteProject.Version);
                    if (v < minimum)
                    {
                        SuggestedVersion = minimum;
                    }
                    else
                    {
                        SuggestedVersion = new System.Version(v.Major, v.Minor, v.Build, v.Revision + 1);
                    }
                }




                CanBeUpdated = HasDifferentHash && (ProjectType == ProjectType.Module || Name == "Panacea");
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

        public string CommitHash { get => _repo?.Head.Tip.Sha; }

        public string CsProjPath { get; }

        public string BasePath { get => Path.GetDirectoryName(CsProjPath); }

        public string FullName { get => Path.GetFileName(CsProjPath).Replace(".csproj", ""); }

        public string Name
        {
            get
            {
                var name = string.Join(".", Path.GetFileName(CsProjPath).Replace(".csproj", "").Split('.').Skip(2));
                if (!string.IsNullOrEmpty(name)) return name;
                return Path.GetFileName(CsProjPath).Replace(".csproj", "").Split('.').Last();
            }
        }

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

        IEnumerable<ProjectFileInfo> Files
        {
            get
            {
                using (var md5 = MD5.Create())
                {
                    foreach (var f in Directory.GetFiles(Path.Combine(BasePath, "bin", "x86", "Release"), "*.*", SearchOption.AllDirectories)
                    .ToList())
                    {
                        var ifo = new ProjectFileInfo() { Name = f };
                        using (var stream = File.OpenRead(f))
                        {
                            var sb = new StringBuilder();
                            foreach (byte b in md5.ComputeHash(stream))
                                sb.Append(b.ToString("x2"));
                            ifo.Md5Hash = sb.ToString();
                        }
                        ifo.Size = new FileInfo(f).Length;
                        yield return ifo;
                    }
                }
            }
        }

        public string ReleaseDate { get; private set; }

        public string BuildBasePath
        {
            get
            {
                return Path.Combine(BasePath, "bin", "x86", "Release");
            }
        }

        public async Task BuildDeltaZip(string path)
        {
            await Task.Run(() =>
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                var zip = new ZipFile
                {
                    ParallelDeflateThreshold = -1
                };
                JsConfig.IncludeNullValues = true;
                ReleaseDate = DateTime.Now.ToString("yyyy-MM-dd");
                var tmp = RemoteProject.Version;
                var tmpSignatures = RemoteProject.Dependencies;
                var tmpFiles = RemoteProject.Files;
                var tmpHash = RemoteProject.CommitHash;

                if (RemoteProject.Files == null)
                {
                    foreach (var file in Files)
                    {
                        var filename = Path.GetFileName(file.Name);
                        var remoteFileName = file.Name.Substring(BuildBasePath.Length + 1, file.Name.Length - BuildBasePath.Length - 1).Replace("\\", "/");
                        var zipfilePath = Path.Combine("files", file.Name.Substring(BuildBasePath.Length + 1, Path.GetDirectoryName(file.Name).Length - BuildBasePath.Length)).Replace("\\", "/");
                        var zipfullName = Path.Combine(zipfilePath, filename).Replace("\\", "/");
                        //if (filename.EndsWith(".dll.config") || filename == "manifest.json") continue;
                        zip.AddFile(file.Name, zipfilePath);
                    }

                }
                else
                {
                    Delta dleta = new Delta()
                    {
                        FromVersion = RemoteProject.Version,
                        ToVersion = SuggestedVersion.ToString(),
                        Module = Name == "Panacea" ? "core" : Name
                    };

                    foreach (var file in Files)
                    {
                        var filename = Path.GetFileName(file.Name);
                        var remoteFileName = file.Name.Substring(BuildBasePath.Length + 1, file.Name.Length - BuildBasePath.Length - 1).Replace("\\", "/");
                        var zipfilePath = Path.Combine("files", file.Name.Substring(BuildBasePath.Length + 1, Path.GetDirectoryName(file.Name).Length - BuildBasePath.Length)).Replace("\\", "/");
                        var zipfullName = Path.Combine(zipfilePath, filename).Replace("\\", "/");
                        //if (filename.EndsWith(".dll.config") || filename == "manifest.json") continue;

                        if (!RemoteProject.Files.Any(rf => rf.Name == remoteFileName))
                        {
                            zip.AddFile(file.Name, zipfilePath);
                            dleta.Added.Add(zipfullName);
                        }
                        else
                        {
                            var f = RemoteProject.Files.First(rf => rf.Name == remoteFileName);
                            if (f.Md5Hash != file.Md5Hash || f.Size != file.Size)
                            {
                                zip.AddFile(file.Name, zipfilePath);
                                dleta.Changed.Add(zipfullName);
                            }
                        }
                    }

                    foreach (var file in RemoteProject.Files)
                    {
                       
                        if (!Files.Any(lf =>
                        {
                            var filename = Path.GetFileName(lf.Name);
                            var remoteFileName = lf.Name.Substring(BuildBasePath.Length + 1, lf.Name.Length - BuildBasePath.Length - 1).Replace("\\", "/");
                            var zipfilePath = Path.Combine("files", lf.Name.Substring(BuildBasePath.Length + 1, Path.GetDirectoryName(lf.Name).Length - BuildBasePath.Length)).Replace("\\", "/");
                            var zipfullName = Path.Combine(zipfilePath, filename).Replace("\\", "/");
                            return file.Name == remoteFileName;
                        }))
                        {
                            dleta.Removed.Add(file.Name);
                        }
                    }

                    File.WriteAllText(string.Format("{0}/deltas.json", path), JsonSerializer.SerializeToString(dleta).IndentJson());
                    zip.AddFile(string.Format("{0}/deltas.json", path), "/");
                }

                var vm = new ProjectsViewModel()
                {
                    Name = ProjectType == ProjectType.Application ? "core" : Name,
                    ProjectHash = CommitHash,
                    Author = "Dotbydot",
                    ReleaseDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    Version = SuggestedVersion.ToString(),
                    Translations = new List<string>(),
                    Files = Files.Select(file =>
                    {
                        var filename = Path.GetFileName(file.Name);
                        var zipfilePath = Path.Combine(file.Name.Substring(BuildBasePath.Length + 1, Path.GetDirectoryName(file.Name).Length - BuildBasePath.Length)).Replace("\\", "/");
                        var zipfullName = Path.Combine(zipfilePath, filename).Replace("\\", "/");
                        return new ProjectFileInfo()
                        {
                            Name = zipfullName,
                            Md5Hash = file.Md5Hash,
                            Size = file.Size
                        };
                    }).ToList(),
                    Dependencies = Dependencies.Select(d => d.Name + "-" + d.Version).ToList(),
                    RedmineVersion = "2.19.6.2"
                };

                File.WriteAllText(string.Format("{0}/manifest.json", path), JsonSerializer.SerializeToString(vm).IndentJson());

                zip.AddFile(string.Format("{0}/manifest.json", path), "/");
                //zip.CompressionLevel = CompressionLevel.Default;
                zip.Save(string.Format("{0}/{1}.zip", path, ProjectType == ProjectType.Application ? "core" : Name));
                zip.Dispose();


                File.Delete(string.Format("{0}/deltas.json", path));
                File.Delete(string.Format("{0}/manifest.json", path));

            });
        }

        public async Task Build()
        {
            if (ProjectType == ProjectType.Module)
                await Builder.Build(this);
            else
            {
                // patch to match Core from previous release flow
                if (Name == "Panacea")
                {
                    await Builder.Build(this);
                }
                else if (Name == "IBT.Updater")
                {
                    var dir = new DirectoryInfo(BasePath);
                    var panacea = dir.Parent.Parent.Parent.FullName;
                    var bin = Path.Combine(panacea, "Panacea", "src", "Panacea", "bin", "x86", "Release", "Updater");
                    if (Directory.Exists(bin))
                        Directory.Delete(bin, true);
                    await Builder.Build(this, bin);
                }
                else
                {
                    var dir = new DirectoryInfo(BasePath);
                    var panacea = dir.Parent.Parent.Parent.FullName;
                    var bin = Path.Combine(panacea, "Panacea", "src", "Panacea", "bin", "x86", "Release", "Applications", Name);

                    if (Directory.Exists(bin))
                        Directory.Delete(bin, true);
                    await Builder.Build(this, bin);
                }
            }
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
