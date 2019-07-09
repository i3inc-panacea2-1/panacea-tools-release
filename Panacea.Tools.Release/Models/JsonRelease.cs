using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Tools.Release.Models
{
    [DataContract]
    public class JsonRelease
    {
        [DataMember(Name = "releaseVersion")]
        public string ReleaseVersion { get; set; }

        [DataMember(Name = "releaseTitle")]
        public string ReleaseTitle { get; set; }

        [DataMember(Name = "releaseCycle")]
        public string ReleaseCycle { get; set; }

        [DataMember(Name = "releaseDate")]
        public string ReleaseDate { get; set; }

        [DataMember(Name = "releaseGeneralFeeling")]
        public string ReleaseGeneralFeeling { get; set; }

        [DataMember(Name = "serverVersion")]
        public string ServerVersion { get; set; }

        [DataMember(Name = "coreClientVersion")]
        public string CoreClientVersion { get; set; }

        [DataMember(Name = "userPluginsVersions")]
        public List<string> UserPluginsVersions { get; set; }

        [DataMember(Name = "BugsResolved")]
        public int BugsResolved { get; set; }

        [DataMember(Name = "TicketsDesign")]
        public int TicketsDesign { get; set; }

        [DataMember(Name = "FeaturesAdded")]
        public int FeaturesAdded { get; set; }

        [DataMember(Name = "TicketsRejected")]
        public int TicketsRejected { get; set; }

        [DataMember(Name = "SupportTicketsHandled")]
        public int SupportTicketsHandled { get; set; }

        [DataMember(Name = "TicketsForRefactoring")]
        public int TicketsForRefactoring { get; set; }

        [DataMember(Name = "ReleaseNotes")]
        public string ReleaseNotes { get; set; }

        [DataMember(Name = "releaseIssuesPerProject")]
        public List<JsonProjectIssues> ReleaseIssuesPerProject { get; set; }

        [DataMember(Name = "releaseRejectedIssues")]
        public List<JsonRejectedIssuesInfo> ReleaseRejectedIssues { get; set; }

    }


    [DataContract]
    public class JsonProjectIssues
    {
        [DataMember(Name = "ProjectName")]
        public string ProjectName { get; set; }

        [DataMember(Name = "releaseProjectInfo")]
        public List<JsonIssuesInfo> ProjectInfo { get; set; }

    }

    [DataContract]
    public class JsonIssuesInfo
    {
        [DataMember(Name = "Title")]
        public string Title { get; set; }

        [DataMember(Name = "ChangelogName")]
        public string ChangelogName { get; set; }

        [DataMember(Name = "TicketId")]
        public int TicketId { get; set; }

        [DataMember(Name = "Tracker")]
        public string Tracker { get; set; }

    }

    

    [DataContract]
    public class JsonRejectedIssuesInfo
    {
        [DataMember(Name = "Title")]
        public string Title { get; set; }

        [DataMember(Name = "Reason")]
        public string Reason { get; set; }

        [DataMember(Name = "TicketId")]
        public int TicketId { get; set; }

    }

}
