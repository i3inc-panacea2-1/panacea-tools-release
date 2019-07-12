using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Tools.Release.Models
{
    [DataContract]
    public class ProjectsViewModel
    {
        [DataMember(Name = "coreVersion")]
        public string CoreVersion { get; set; } = "2.0.12.52";

        [DataMember(Name = "file")]
        public string File { get; set; } = "deprecated";

        [DataMember(Name = "ns")]
        public string Namespace { get; set; } = "deprecated";

        [DataMember(Name = "classname")]
        public string ClassName { get; set; } = "deprecated";

        [DataMember(Name = "files")]
        public List<ProjectFileInfo> Files { get; set; }

        [DataMember(Name = "translations")]
        public List<string> Translations { get; set; }

        [DataMember(Name = "author")]
        public string Author { get; set; }

        [DataMember(Name = "projectHash")]
        public string ProjectHash { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "releaseDate")]
        public string ReleaseDate { get; set; }

        [DataMember(Name = "signatures")]
        public List<string> Dependencies { get; set; }


        [DataMember(Name = "redmineVersion")]
        public string RedmineVersion { get; set; }
    }
}
