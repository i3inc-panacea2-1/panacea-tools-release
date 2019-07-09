using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Redmine.Net.Api.Types;
using Version = System.Version;
using System.IO;
using Ionic.Zip;
using ServiceStack.Text;
using System.ComponentModel;
using System.Windows.Media;
using Ionic.Zlib;

namespace Panacea.Tools.Release.Models
{
    [DataContract]
    public class ProjectInfo : INotifyPropertyChanged
    {
        public ProjectInfo()
        {
            Errors = new List<string>();
            CurrentSignatures = new List<string>();
            IncludedSubfoldersForPackage = new List<string>();
            CoreVersion = "2.0.12.52";
        }

      
        public string ProjectHash { get; set; }

        [DataMember(Name = "projectHash")]
        public string RemoteProjectHash { get; set; }


        [DataMember(Name = "author")]
        public string Author { get; set; }


        [DataMember(Name = "coreVersion")]
        public string CoreVersion { get; set; }

        [DataMember(Name = "releaseDate")]
        public string ReleaseDate { get; set; }



        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "version")]
        public string RemoteVersion { get; set; }

        [DataMember(Name = "classname")]
        public string ClassName { get; set; }

        [DataMember(Name = "ns")]
        public string Namespace { get; set; }

        [DataMember(Name = "file")]
        public string FileName { get; set; }

        [DataMember(Name = "translations")]
        public List<string> Translations { get; set; }

        [DataMember(Name = "files")]
        public List<ProjectFileInfo> RemoteFiles { get; set; }

        public Brush Background
        {
            get
            {
                if (CurrentVersion != RemoteVersion)
                    return Brushes.Red;

                if (RequiresUpdate && Bugs == 0 && Features == 0 && Refactors == 0)
                    return Brushes.Orange;

                if (!RequiresUpdate) return Brushes.Azure;

                return Brushes.Transparent;
            }
        }
        public List<string> CurrentSignatures { get; set; }

        public List<ProjectFileInfo> LocalFiles { get; set; }

        public string CurrentVersion { get; set; }

        [DataMember(Name = "signatures")]
        public List<string> PreviousSignatures { get; set; }

        public bool HasChanges { get; set; }

        public string BuildDirectory { get; set; }

        public bool ManifestChanged { get; set; }

        public string ProjectDirectory { get; set; }
        public bool SignaturesChanged
        {
            get
            {
                if (PreviousSignatures == null) return true;
                return !PreviousSignatures.OrderBy(a=>a).SequenceEqual(CurrentSignatures.OrderBy(a=>a));
            }
        }

        public bool CompatibilityChanged { get; set; }
        public List<string> Errors { get; set; }

        public List<string> IncludedSubfoldersForPackage { get; set; }
        public bool RequiresUpdate
        {
            get
            {
                return HasDifferentProjectHash || ManifestChanged || CompatibilityChanged;
            }
        }

        public List<Issue> Issues { get; set; }

        [DataMember(Name = "redmineVersion")]
        public string RedmineVersion { get; set; }

        bool _hasDifferencesInFilesCalculated = false;
        bool _hasDifferencesInFiles = false;
        public bool HasDifferencesInFiles
        {
            get
            {
                if (!_hasDifferencesInFilesCalculated)
                {
                 
                    if (RemoteFiles == null || LocalFiles == null) return true;
                    _hasDifferencesInFilesCalculated = true;
                    _hasDifferencesInFiles = !this.LocalFiles.OrderBy(lf => lf.Name).SequenceEqual(RemoteFiles.OrderBy(rf => rf.Name));
                }
                return _hasDifferencesInFiles;
            }
        }

        public bool HasDifferentProjectHash
        {
            get
            {
                return RemoteProjectHash != ProjectHash;
            }
        }


        bool _update = false;
        public bool Update
        {
            get
            {
                return _update;
            }

            set
            {
                _update = value;
                OnPropertyChanged("Update");
            }
        }
        string _suggestedVersion = null;
        public string SuggestedVersion
        {
            get
            {
                if (RequiresUpdate && _suggestedVersion==null)
                {
                    if (string.IsNullOrEmpty(CurrentVersion)) return "";
                    var vers = Version.Parse(CurrentVersion);
                    if(!string.IsNullOrEmpty(RemoteVersion))
                        vers = Version.Parse(RemoteVersion);
                    if (Bugs > 0 || Refactors > 0)
                    {
                        vers = new Version(vers.Major, vers.Minor, vers.Build, vers.Revision+1);
                    }
                    if (Features > 0)
                    {
                        vers = new Version(vers.Major, vers.Minor, vers.Build + 1, 0);
                    }
                    
                    if (vers.ToString() == RemoteVersion)
                    {
                        var rvers = Version.Parse(RemoteVersion);
                        vers = new Version(rvers.Major, rvers.Minor, rvers.Build, rvers.Revision + 1);
                    }
                    if (String.IsNullOrEmpty(RemoteVersion) )
                    {
                        vers = new Version(2, 0, 0, 0);
                    }
                    _suggestedVersion=vers.ToString();
                    return _suggestedVersion;
                }
                return _suggestedVersion;
            }
            set
            {
                _suggestedVersion = value;
                OnPropertyChanged("SuggestedVersion");
            }
        }

        public int Bugs
        {
            get
            {
                if (Issues != null && Issues.Any(iss => iss.Tracker != null && iss.Tracker.Id == 1))
                    return Issues.Count(iss => iss.Tracker != null && iss.Tracker.Id == 1);
                return 0;
            }
        }
        public int Features
        {
            get
            {
                if (Issues != null && Issues.Any(iss => iss.Tracker != null && iss.Tracker.Id == 2))
                    return Issues.Count(iss => iss.Tracker != null && iss.Tracker.Id == 2);
                return 0;
            }
        }
        public int Refactors
        {
            get
            {
                if (Issues != null && Issues.Any(iss => iss.Tracker != null && iss.Tracker.Id == 9))
                    return Issues.Count(iss => iss.Tracker != null && iss.Tracker.Id == 9);
                return 0;
            }
        }
        public List<Ticket> FeaturesInfo
        {
            get
            {
                var list = new List<Ticket>();
                if (Issues != null && Issues.Any(iss => iss.Tracker != null && iss.Tracker.Id == 2))
                {
                   
                    foreach (var iss in Issues.Where(iss => iss.Tracker != null && iss.Tracker.Id == 2))
                    {
                        list.Add(new Ticket() { Id = iss.Id.ToString(), Subject = iss.Subject, Description = iss.Description });
                       
                    }
                    
                }
                return list;
            }
        }
        public List<Ticket> BugsInfo
        {
            get
            {

                var list = new List<Ticket>();
                if (Issues != null && Issues.Any(iss => iss.Tracker != null && iss.Tracker.Id == 1))
                {

                    foreach (var iss in Issues.Where(iss => iss.Tracker != null && iss.Tracker.Id == 1))
                    {
                        list.Add(new Ticket() { Id = iss.Id.ToString(), Subject = iss.Subject, Description = iss.Description });

                    }

                }
                return list;
            }
        }

        public List<Ticket> RefactoringInfo
        {
            get
            {

                var list = new List<Ticket>();
                if (Issues != null && Issues.Any(iss => iss.Tracker != null && iss.Tracker.Id == 9))
                {

                    foreach (var iss in Issues.Where(iss => iss.Tracker != null && iss.Tracker.Id == 9))
                    {
                        list.Add(new Ticket() { Id = iss.Id.ToString(), Subject = iss.Subject, Description = iss.Description });

                    }

                }
                return list;
            }
        }

        public async Task BuildDeltaZip(string path)
        {
            await Task.Run(() =>
            {
                var zip = new ZipFile();
                zip.ParallelDeflateThreshold = -1;
                JsConfig.IncludeNullValues = true;
                this.ReleaseDate = DateTime.Now.ToString("yyyy-MM-dd");
                var tmp = RemoteVersion;
                var tmpSignatures = PreviousSignatures;
                var tmpFiles = RemoteFiles;
                var tmpHash = RemoteProjectHash;

                RemoteProjectHash = ProjectHash;

                PreviousSignatures = CurrentSignatures;

                //zip.AddFile(BaseDirectory + "manifest.json", "/");
                

                if (RemoteFiles == null)
                {

                    foreach (var file in LocalFiles)
                    {
                        var filename = Path.GetFileName(file.Name);
                        //if (filename.EndsWith(".dll.config") || filename == "manifest.json") continue;
                        zip.AddFile(BuildDirectory + file.Name, Path.Combine("files", Path.GetDirectoryName(file.Name)));
                    }

                }
                else
                {
                    Delta dleta = new Delta()
                    {
                        FromVersion = RemoteVersion,
                        ToVersion = SuggestedVersion,
                        Module=Name
                    };
                    
                    foreach (var file in LocalFiles)
                    {
                        var filename = Path.GetFileName(file.Name);

                        //if (filename.EndsWith(".dll.config") || filename == "manifest.json") continue;

                        if (!RemoteFiles.Any(rf => rf.Name == file.Name))
                        {
                            zip.AddFile(BuildDirectory + file.Name, Path.Combine("files", Path.GetDirectoryName(file.Name)));
                            dleta.Added.Add(Path.Combine("files", Path.GetDirectoryName(file.Name)).Replace("\\", "/") + "/" + Path.GetFileName(file.Name));
                        }
                        else
                        {
                            var f = RemoteFiles.First(rf => rf.Name == file.Name);
                            if (f.Md5Hash != file.Md5Hash || f.Size != file.Size)
                            {
                                zip.AddFile(BuildDirectory + file.Name, Path.Combine("files", Path.GetDirectoryName(file.Name)));
                                dleta.Changed.Add(Path.Combine("files", Path.GetDirectoryName(file.Name)).Replace("\\", "/") + "/" + Path.GetFileName(file.Name));
                            }
                        }
                    }
                    
                    if (RemoteFiles.Any(rf => !LocalFiles.Any(lf => lf.Name == rf.Name)))
                    {
                        RemoteFiles.Where(rf => !LocalFiles.Any(lf => lf.Name == rf.Name)).ToList().ForEach(nf =>
                        {
                            
                                dleta.Removed.Add(Path.Combine("files", Path.GetDirectoryName(nf.Name)).Replace("\\","/") + "/" + Path.GetFileName(nf.Name));
                        });
                    }
                    File.WriteAllText(String.Format("{0}/deltas.json", path), JsonSerializer.SerializeToString<Delta>(dleta));
                    zip.AddFile(String.Format("{0}/deltas.json", path), "/");
                }
                RemoteFiles = LocalFiles;


                this.RemoteVersion = SuggestedVersion;


                File.WriteAllText(String.Format("{0}/manifest.json", path), JsonSerializer.SerializeToString<ProjectInfo>(this));

                zip.AddFile(String.Format("{0}/manifest.json", path), "/");
                //zip.CompressionLevel = CompressionLevel.Default;
                zip.Save(String.Format("{0}/{1}.zip", path, Name));
                zip.Dispose();

                RemoteFiles = tmpFiles;
                File.Delete(String.Format("{0}/deltas.json", path));
                File.Delete(String.Format("{0}/manifest.json", path));
                RemoteProjectHash = tmpHash;
                this.RemoteVersion = tmp;
                PreviousSignatures = tmpSignatures;
            });
        }

        public async Task SetAssemblyVersion()
        {
            await Task.Run(() =>
            {
                var file = Path.Combine(ProjectDirectory, "Properties\\AssemblyInfo.cs");
                if (File.Exists(file))
                {
                    List<String> lines = File.ReadLines(file, Encoding.UTF8).ToList();
                    Boolean FileVersionAdded = false;
                    using (StreamWriter sw = new StreamWriter(File.Open(file, FileMode.OpenOrCreate)))
                    {
                        foreach (String line in lines)
                        {
                            if (line.Contains("AssemblyVersion") && !line.StartsWith("//"))
                            {
                                sw.Write("[assembly: AssemblyVersion(\"" + SuggestedVersion + "\")]" + Environment.NewLine);
                            }
                            else if (line.Contains("AssemblyFileVersion") && !line.StartsWith("//"))
                            {
                                FileVersionAdded = true;
                                sw.Write("[assembly: AssemblyFileVersion(\"" + SuggestedVersion + "\")]" + Environment.NewLine);
                            }
                            else
                            {
                                sw.Write(line + Environment.NewLine);
                            }
                        }
                        if (!FileVersionAdded)
                            sw.Write("[assembly: AssemblyFileVersion(\"" + SuggestedVersion + "\")]" + Environment.NewLine);

                        //to vs exei bug mi to vgaleis
                        sw.Write(Environment.NewLine + Environment.NewLine);
                    }
                }
            });
        }

        public bool EqualsExcludeTranslations(object obj)
        {
            if (obj == this) return true;
            if (!(obj is ProjectInfo)) return false;

            var comp = obj as ProjectInfo;
            
            return comp.Namespace == this.Namespace
                   && comp.ClassName == this.ClassName && comp.FileName == this.FileName;
        }

        public override bool Equals(object obj)
        {
            if (obj == this) return true;
            if (!(obj is ProjectInfo)) return false;

            var comp = obj as ProjectInfo;
            if (Translations == null && comp.Translations != null) return false;
            if (Translations != null && comp.Translations != null)
            {
                if(!comp.Translations.OrderBy(c=>c).ToList().SequenceEqual(Translations.OrderBy(c=>c).ToList()))
                {
                    return false;
                }
            }
            return comp.Namespace == this.Namespace
                   && comp.ClassName == this.ClassName && comp.FileName == this.FileName;

        }

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    
    public class Delta
    {
        public Delta()
        {
            Changed = new List<string>();
            Added = new List<string>(); 
            Removed = new List<string>();
        }

        public string Module { get; set; }
        public string FromVersion { get; set; }
        public string ToVersion { get; set; }
        public List<string> Changed { get; set; }
        public List<string> Added { get; set; }

        public List<string> Removed { get; set; }
    }

    [DataContract]
    public class ProjectFileInfo
    {
        [DataMember(Name = "filename")]
        public string Name { get; set; }

        [DataMember(Name = "download_from")]
        public string DownloadPath { get; set; }

        [DataMember(Name = "hash")]
        public string Md5Hash { get; set; }

        [DataMember(Name = "size")]
        public long Size { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is ProjectFileInfo)) return false;

            var comp = obj as ProjectFileInfo;
            return comp.Md5Hash == this.Md5Hash && comp.Size == this.Size;
        }
    }
}
