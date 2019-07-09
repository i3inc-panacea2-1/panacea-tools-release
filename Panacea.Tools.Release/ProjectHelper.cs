using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Mono.Cecil;
using Panacea.Tools.Release.Models;
using ServiceStack.Text;

namespace Panacea.Tools.Release
{
    public class ProjectHelper
    {
        private SolutionInfo _info;
        private List<ProjectInfo> _projectInfo;
        private ProjectInfo _coreInfo;

        public ProjectInfo CoreProjectInfo
        {
            get
            {
                return _coreInfo;
            }
        }

        public List<ProjectInfo> PluginsProjectInfo
        {
            get
            {
                return _projectInfo;
            }
        }

        public ProjectHelper(SolutionInfo info)
        {
            _info = info;
            _projectInfo =  new List<ProjectInfo>();
        }

        private static void RecursivelyAnalyzeType(TypeDefinition type, ref ProjectInfo info)
        {
            if (type.HasInterfaces)
            {
                
                if (type.Interfaces.Any(i => i.Scope.Name == "PanaceaLib"))
                {
                    var list = type.Interfaces.Where(i => i.Scope.Name == "PanaceaLib").ToList();
                    foreach (var decl in from item in list from decl in item.Resolve().Methods where decl.IsPublic select decl)
                    {
                        info.CurrentSignatures.Add(decl.FullName);
                    };

                }
            }
            if (type.BaseType == null || type.BaseType.Scope.Name != "PanaceaLib") return;
           
            info.CurrentSignatures.Add(String.Format("{0}", type.BaseType.Resolve().FullName));
            var list1 = type.BaseType.Resolve().Methods;
            foreach (var item in list1.Where(item => item.IsPublic))
            {
                info.CurrentSignatures.Add(String.Format("{0}", item.FullName.Replace("!!0", "T")));
            }
            RecursivelyAnalyzeType(type.BaseType.Resolve(), ref info);
        }

        public Task BuildSignatures()
        {
            return Task.Run(() =>
            {
                
                MessageHelper.OnMessage("Getting signatures...");
                for (var i = 0; i <_projectInfo.Count; i++)
                {

                    var info = _projectInfo[i];
                    var resolver = new DefaultAssemblyResolver();
                    resolver.AddSearchDirectory(Path.Combine(_info.CoreBuildPath, "Lib"));


                    var parameters = new ReaderParameters
                    {
                        AssemblyResolver = resolver,
                    };
                    var manifestFile = String.Format("{0}/{1}/manifest.json", _info.PluginsBuildPath, info.Name);
                    if (!File.Exists(manifestFile))
                    {
                        throw new Exception(String.Format("No manifest for plugin '{0}'. Application will now exit", info.Name));
                    }
                    var manifest = JsonSerializer.DeserializeFromString<ProjectInfo>(File.ReadAllText(manifestFile));
                    if (manifest.Name != info.Name)
                    {
                        throw new Exception(String.Format("Names mismatch. Manifest: {0}, Project: {1}", manifest.Name, info.Name));
                    }
                    var ad = AssemblyDefinition.ReadAssembly(String.Format("{0}/{1}/{2}", _info.PluginsBuildPath, info.Name, manifest.FileName), parameters);
                    foreach (var resource in
ad.MainModule.Resources.OfType<EmbeddedResource>())
                    {

                        Console.WriteLine(resource.Name);
                    }
                    foreach (var type in ad.MainModule.Types)
                    {
                        RecursivelyAnalyzeType(type, ref info);
                    }

                    //Console.WriteLine("---- Method References ---");
                    foreach (var type in ad.MainModule.GetMemberReferences().Where(type => type.DeclaringType.Scope.Name == "PanaceaLib"))
                    {
                        try
                        {
                            info.CurrentSignatures.Add(type.DeclaringType.Resolve()
                                .Methods.Any(m => m.Name == type.Name)
                                ? type.DeclaringType.Resolve()
                                    .Methods.First(m => m.Name == type.Name)
                                    .FullName.Replace("!!0", "T")
                                : type.DeclaringType.Resolve()
                                    .Fields.First(m => m.Name == type.Name)
                                    .FullName.Replace("!!0", "T"));
                        }
                        catch
                        {
                        }
                    }
                    if (info.CurrentSignatures.Count > 0)
                        info.CurrentSignatures = info.CurrentSignatures.OrderBy(s => s).Distinct().ToList();
                }
            });
        }

        public Task BuildCoreSignatures()
        {
            return Task.Run(() =>
            {
                MessageHelper.OnMessage("Getting Core signatures...");
                var resolver = new DefaultAssemblyResolver();
                //resolver.AddSearchDirectory(_info.CoreBuildPath);
                resolver.AddSearchDirectory(Path.Combine(_info.CoreBuildPath, "Lib"));
                var parameters = new ReaderParameters
                {
                    AssemblyResolver = resolver,
                };

                var ad = AssemblyDefinition.ReadAssembly(String.Format("{0}/Lib/{1}", _info.CoreBuildPath, ConfigurationManager.AppSettings["sharedLibName"]), parameters);

                foreach (var type in ad.MainModule.Types.Where(type => type.IsPublic))
                {
                    if(type.Name == "RawKeyboard") Console.WriteLine();
                    _coreInfo.CurrentSignatures.Add(type.FullName);
                    foreach (var mi in type.Resolve().Methods.Where(mi=>!mi.IsPrivate))
                    {
                        _coreInfo.CurrentSignatures.Add(mi.FullName);
                    }
                    foreach (var mi in type.Resolve().Fields.Where(mi => !mi.IsPrivate))
                    {
                        _coreInfo.CurrentSignatures.Add(mi.FullName);
                    }
                    foreach (var mi in type.Resolve().NestedTypes.Where((mi1 => mi1.IsNestedPublic)))
                    {
                        _coreInfo.CurrentSignatures.Add(mi.FullName);
                        foreach (var mi2 in mi.Resolve().Methods.Where(mi3 => !mi3.IsPrivate))
                        {
                            _coreInfo.CurrentSignatures.Add(mi2.FullName);
                        }
                    }
                }
                if (_coreInfo.CurrentSignatures.Count > 0)
                    _coreInfo.CurrentSignatures = _coreInfo.CurrentSignatures.OrderBy(s => s).Distinct().ToList();
                var sw = new StreamWriter("core-signatures.txt");
                sw.Write(String.Join(Environment.NewLine, _coreInfo.CurrentSignatures));
                sw.Close();
            });
        }

        public Task GetPluginProjectInfo()
        {
            return Task.Run(() =>
            {
                using (var client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    foreach (var name in _info.ProjectNames)
                    {
                        try
                        {
                            MessageHelper.OnMessage(String.Format("Fetching information for {0}", name));
                            var manifestFile = String.Format("{0}/{1}/manifest.json", _info.PluginsBuildPath, name);
                            if (!File.Exists(manifestFile))
                            {
                                MessageBox.Show(String.Format("No manifest for plugin '{0}'. Application will now exit", name));
                                continue;
                            }
                            var manifest = JsonSerializer.DeserializeFromString<ProjectInfo>(File.ReadAllText(manifestFile, Encoding.UTF8));


                            var result = client.DownloadString(new Uri(ConfigurationManager.AppSettings["server"] + String.Format("get_module_manifest/{0}/", name)));
                            var info = JsonSerializer.DeserializeFromString<ProjectInfo>(result);
                            if (String.IsNullOrEmpty(info.Name))
                            {
                                if (manifest.Name != name)
                                {
                                    throw new Exception(String.Format("Names mismatch. Manifest: {0}, Project: {1}", manifest.Name, name));
                                }
                                info = manifest;
                            }
                            else if (!info.EqualsExcludeTranslations(manifest))
                            {
                                info.Author = manifest.Author;
                                info.FileName = manifest.FileName;
                                info.ClassName = manifest.ClassName;
                                info.Namespace = manifest.Namespace;
                                info.ManifestChanged = true;
                            }
                            info.Translations.AddRange(manifest.Translations);
                            info.Translations = info.Translations.Distinct().ToList();
                            info.BuildDirectory = String.Format("{0}/{1}/", _info.PluginsBuildPath, info.Name);
                            info.ProjectDirectory = String.Format("{0}/{1}/", _info.PluginsPath, info.Name);
                            info.ProjectHash = CreateMd5ForFolder(info.ProjectDirectory);
                            if (!Directory.Exists(info.ProjectDirectory))
                            {
                                throw new Exception(String.Format("WTF is going on with {0}?", name));
                            }
                            _projectInfo.Add(info);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(String.Format("Unknown error for plugin {0}  ", ex.Message));
                        }
                    }
                }
            });
        }

        public Task GetCoreProjectInfo()
        {
            return Task.Run(() =>
            {
                MessageHelper.OnMessage(String.Format("Fetching information for {0}", "Core"));
                const string core = "core";
                var manifestFileCore = String.Format("{0}/manifest.json", _info.CoreBuildPath);
                if (!File.Exists(manifestFileCore))
                {
                    throw new Exception(String.Format("No manifest for '{0}'. Application will now exit", core));
                }
                var manif = JsonSerializer.DeserializeFromString<ProjectInfo>(File.ReadAllText(manifestFileCore));
                using (var client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    var res = client.DownloadString(new Uri(ConfigurationManager.AppSettings["server"] + String.Format("get_module_manifest/{0}/", core)));
                    var coreInfo = JsonSerializer.DeserializeFromString<ProjectInfo>(res);
                    if (String.IsNullOrEmpty(coreInfo.Name))
                    {
                        if (manif.Name != core)
                        {
                            throw new Exception(String.Format("Names mismatch. Manifest: {0}, Project: {1}", manif.Name, core));
                        }
                        coreInfo = manif;
                    }
                    else if (!coreInfo.EqualsExcludeTranslations(manif))
                    {
                        coreInfo.Author = manif.Author;
                        coreInfo.Translations = manif.Translations;
                        coreInfo.FileName = manif.FileName;
                        coreInfo.ClassName = manif.ClassName;
                        coreInfo.Namespace = manif.Namespace;
                        coreInfo.ManifestChanged = true;
                    }
                    coreInfo.ProjectDirectory = String.Format("{0}/{1}/{2}/", _info.CorePath, "Application", "Panacea");
                    coreInfo.BuildDirectory = String.Format("{0}/", _info.CoreBuildPath);
                    coreInfo.ProjectHash = CreateMd5ForFolder(coreInfo.ProjectDirectory);
                    if (!Directory.Exists(coreInfo.ProjectDirectory))
                    {
                        throw new Exception(String.Format("WTF is going on with {0}?", "Core"));
                    }
                    coreInfo.IncludedSubfoldersForPackage = new List<string>(ConfigurationManager.AppSettings["coreSubFolders"].Split(','));
                    _coreInfo = coreInfo;
                }
            });
        }

        public Task CalculateLocalFiles()
        {
            return Task.Run(() =>
            {
                MessageHelper.OnMessage("Comparing files...");
                foreach (var info in _projectInfo)
                {

                    if (!Directory.Exists(String.Format("{0}/{1}/", _info.PluginsBuildPath, info.Name)))
                    {
                        continue;
                    }

                    //CHECK VERSIONS
                    try
                    {
                        var manifestFile = String.Format("{0}/{1}/manifest.json", _info.PluginsBuildPath, info.Name);
                        var manifest = JsonSerializer.DeserializeFromString<ProjectInfo>(File.ReadAllText(manifestFile));
                        if (manifest.Name != info.Name)
                        {
                            throw new Exception(String.Format("Names mismatch. Manifest: {0}, Project: {1}", manifest.Name, info.Name));
                        }
                        var filename = String.Format("{0}/{1}/{2}", _info.PluginsBuildPath, info.Name, manifest.FileName);

                        if (File.Exists(filename))
                        {
                            var fvi = FileVersionInfo.GetVersionInfo(filename);
                            info.CurrentVersion = fvi.ProductVersion.ToString();
                            
                            if (fvi.ProductVersion.ToString() != info.RemoteVersion)
                            {
                                info.Errors.Add("Versions mismatch");
                            }
                        }
                        else info.Errors.Add("File not found");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(String.Format("Exception for plugin '{0}' ({1}). Application will now exit", info.Name, ex.Message));
                    }

                    try
                    {
                        var localFiles = new List<ProjectFileInfo>();
                        using (var md5 = MD5.Create())
                        {
                            var dir = $"{_info.PluginsBuildPath}/{info.Name}/";
                            var path = Path.GetDirectoryName($"{_info.PluginsBuildPath}/{info.Name}/");
                            foreach (var filename in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                            {

                                if (Path.GetFileName(filename).EndsWith(".dll.config")) continue;
                                if (Path.GetFileName(filename) == "manifest.json" &&
                                    Path.GetDirectoryName(filename) == path) continue;
                                var localFile = new ProjectFileInfo()
                                {
                                    Name = filename.Remove(0, dir.Length).Replace('\\', '/')
                                };
                                using (var stream = File.OpenRead(filename))
                                {
                                    var sb = new StringBuilder();
                                    foreach (byte b in md5.ComputeHash(stream))
                                        sb.Append(b.ToString("x2"));
                                    localFile.Md5Hash = sb.ToString();
                                }
                                localFile.Size = new FileInfo(filename).Length;
                                localFiles.Add(localFile);
                            }
                        }

                        info.LocalFiles = localFiles;
                    }
                    catch
                    {
                        //ignore
                    }
                    info.ProjectHash = CreateMd5ForFolder(info.ProjectDirectory);
                }
            });
        }

        public Task CalculateLocalCoreFiles()
        {
            return Task.Run(() =>
            {
                MessageHelper.OnMessage("Comparing core files...");
                try
                {
                    var manifestFile = String.Format("{0}/manifest.json", _info.CoreBuildPath);

                    var manifest = JsonSerializer.DeserializeFromString<ProjectInfo>(File.ReadAllText(manifestFile));
                    if (manifest.Name != _coreInfo.Name)
                    {
                        throw new Exception(String.Format("Names mismatch. Manifest: {0}, Project: {1}", manifest.Name, _coreInfo.Name));
                    }
                    var filename = String.Format("{0}/{1}", _info.CoreBuildPath, "Panacea.exe");

                    if (File.Exists(filename))
                    {
                        var fvi = FileVersionInfo.GetVersionInfo(filename);
                        _coreInfo.CurrentVersion = fvi.ProductVersion.ToString();

                        if (fvi.ProductVersion.ToString() != _coreInfo.RemoteVersion)
                        {
                            _coreInfo.Errors.Add("Versions mismatch");
                        }
                    }
                    else _coreInfo.Errors.Add("File not found");
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("Exception for plugin '{0}' ({1}). Application will now exit", _coreInfo.Name, ex.Message));
                }
                try
                {
                    var localFiles = new List<ProjectFileInfo>();
                    using (var md5 = MD5.Create())
                    {
                        string dir = String.Format("{0}/", _info.CoreBuildPath);
                        foreach (var filename in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                        {
                            if (_coreInfo.IncludedSubfoldersForPackage.Count > 0)
                            {
                                if (dir.Length <= Path.GetDirectoryName(filename).Length - 1)
                                {
                                    var foldername =
                                        Path.GetDirectoryName(filename).Substring(dir.Length).Split('\\')[0];
                                    if (!String.IsNullOrEmpty(foldername) &&
                                        !_coreInfo.IncludedSubfoldersForPackage.Contains(foldername))
                                    {

                                        continue;
                                    }
                                }
                            }
                            //if (Path.GetFileName(filename) == "manifest.json" ||
                            //    Path.GetFileName(filename).EndsWith(".dll.config") ||
                            //    Path.GetFileName(filename).EndsWith(".pdb")
                            //    ) continue;
                            var localFile = new ProjectFileInfo()
                            {
                                Name = filename.Remove(0, dir.Length).Replace('\\', '/')
                            };
                            using (var stream = File.OpenRead(filename))
                            {
                                var sb = new StringBuilder();
                                foreach (byte b in md5.ComputeHash(stream))
                                    sb.Append(b.ToString("x2"));
                                localFile.Md5Hash = sb.ToString();
                            }
                            localFile.Size = new FileInfo(filename).Length;
                            localFiles.Add(localFile);
                        }
                    }
                    _coreInfo.LocalFiles = localFiles;
                }
                catch
                {
                    //ignore
                }
                _coreInfo.ProjectHash = CreateMd5ForFolder(_coreInfo.ProjectDirectory);
            });
        }

        public Task CheckCompatibilityOfPreviousPluginsWithNewCore()
        {
            return Task.Run(() =>
            {
                MessageHelper.OnMessage("Comparing signatures...");
                foreach (var info in _projectInfo)
                {
                    var previouscompatible = true;
                    if (info.PreviousSignatures != null && info.PreviousSignatures.Count > 0)
                    {
                        foreach (var signature in info.PreviousSignatures)
                        {
                            if (!_coreInfo.CurrentSignatures.Contains(signature))
                            {
                                previouscompatible = false;
                                break;
                            }
                        }
                    }
                    else previouscompatible = false;

                    info.CompatibilityChanged = !previouscompatible;
                }
            });
        }

        public Task CheckCompatibilityOfNewPluginsWithNewCore()
        {
            return Task.Run(() =>
            {
                MessageHelper.OnMessage("Comparing signatures...");
                foreach (var info in _projectInfo)
                {
                    bool previouscompatible = true;
                    if (info.CurrentSignatures != null && info.CurrentSignatures.Count > 0)
                    {
                        foreach (var signature in info.CurrentSignatures)
                        {
                            if (!_coreInfo.CurrentSignatures.Contains(signature))
                            {
                                Utils.Panic("A new plugin is incompatible with the new core. Something is really bad. {0} {1}", info.Name, signature);
                            }
                        }
                    }
                    else previouscompatible = false;

                    if (!previouscompatible)
                        info.CompatibilityChanged = true;
                }
            });
        }

        public static string CreateMd5ForFolder(string path)
        {
            var dirs = Directory.GetDirectories(path, "obj", SearchOption.AllDirectories);
            if (dirs.Length > 0)
            {
                foreach (var dir in dirs)
                {
                    Directory.Delete(dir, true);
                }
                
            }
            dirs = Directory.GetDirectories(path, "bin", SearchOption.AllDirectories);
            if (dirs.Length> 0)
            {
                foreach (var dir in dirs)
                {
                    Directory.Delete(dir, true);
                }
            }
            // assuming you want to include nested folders
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                 .OrderBy(p => p).ToList();

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
    }
}
