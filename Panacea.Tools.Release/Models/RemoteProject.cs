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
    public class RemoteProject 
    {
        [DataMember(Name = "projectHash")]
        public string CommitHash { get; set; }

        [DataMember(Name = "author")]
        public string Author { get; set; }

        [DataMember(Name = "releaseDate")]
        public string ReleaseDate { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "translations")]
        public List<string> Translations { get; set; }

        [DataMember(Name = "files")]
        public List<ProjectFileInfo> Files { get; set; }

        [DataMember(Name = "signatures")]
        public List<string> Dependencies { get; set; }
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
