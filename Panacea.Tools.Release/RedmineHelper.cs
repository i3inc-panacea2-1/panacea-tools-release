using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Redmine.Net.Api;
using Redmine.Net.Api.Types;
using ServiceStack.Text;
using System.Windows;
using ServiceStack;
using Panacea.Tools.Release.Models;

namespace Panacea.Tools.Release
{
    public static class RedmineHelper
    {
        static int _supportIssuesCount = 0;
        private static List<Issue> issues, rejectedIssues, designTickets;
        public static string RedmineVersion;
        private static RedmineWebClient rc;
        private static RedmineManager rm;
        static RedmineHelper()
        {
            ServicePointManager.ServerCertificateValidationCallback =
                    ((sender2, certificate, chain, sslPolicyErrors) => true);
            rc = new RedmineWebClient();
            rm = new RedmineManager("https://redmine.dotbydot.eu", "00a4769287faca4f7dc735fc48101699d6284dfe");
        }

        public static Task AssignTicketsToProject(ProjectInfo info)
        {
            MessageHelper.OnMessage("Analyzing issues...");
            return Task.Run(() =>
            {
                if (
                    issues.Any(
                        iss =>
                            iss.CustomFields.Any(
                                cf =>
                                    cf.Name == "Package" && cf.Values.Count > 0 &&
                                    cf.Values.Any(
                                        n =>
                                            n.ToString()
                                                .Replace(" ", "")
                                                .Trim()
                                                .Equals(info.Name, StringComparison.OrdinalIgnoreCase)))))
                {
                    info.Issues = issues.Where(
                        iss =>
                            iss.CustomFields.Any(
                                cf =>
                                    cf.Name == "Package" && cf.Values.Count > 0 &&
                                    cf.Values.Any(
                                        n =>
                                            n.ToString()
                                                .Replace(" ", "")
                                                .Trim()
                                                .Equals(info.Name, StringComparison.OrdinalIgnoreCase)))).ToList();
                }

            });
        }

        public static Task GetChangesFromRedmine()
        {
            return Task.Run(() =>
            {

                var parameters = new NameValueCollection { { "include", "relations" } };
                var redProject = rm.GetObject<Project>("50", parameters);
                var versions = rm.GetObjectList<Redmine.Net.Api.Types.Version>(new NameValueCollection
                        {
                            {"include", "relations"},
                            {"project_id", redProject.Id.ToString()}
                        }).ToList();

                versions = versions.Where(v => v.DueDate > DateTime.Now.Subtract(TimeSpan.FromDays(1))).OrderBy(v => v.DueDate).ToList();

                Redmine.Net.Api.Types.Version selectedVersion = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var pickVersion = new SelectVersion(versions);
                    pickVersion.Owner = MainWindow.Instance;
                    pickVersion.ShowDialog();

                    selectedVersion = pickVersion.Version;
                    RedmineVersion = pickVersion.Version.Name;
                });

                rm.PageSize = 9998;
                int offset = 0;
                issues = rejectedIssues = designTickets = new List<Issue>();
                var issues_ =
                    rm.GetObjectList<Issue>(new NameValueCollection
                    {
                        {"include", "relations"},
                        {"status_id", "5"},
                        {"project_id", redProject.Id.ToString()},
                        {"fixed_version_id" , selectedVersion.Id.ToString()}
                    });

                while (issues_.Count > 0)
                {
                    issues = issues.Concat(issues_).ToList();
                    offset += 25;
                    issues_ =
                        rm.GetObjectList<Issue>(new NameValueCollection
                        {
                            {"include", "relations"},
                            {"status_id", "5"},
                            {"offset", offset.ToString()},
                            {"project_id", redProject.Id.ToString()},
                            {"fixed_version_id" , selectedVersion.Id.ToString()}
                        });
                }
                if (issues.Any(i => i.Tracker.Id == 4))
                {
                    designTickets = issues.Where(i => i.Tracker.Id == 4).ToList();
                    issues = issues.Where(i => i.Tracker.Id != 4).ToList();
                }

                //rejected
                offset = 0;
                _rejectedIssues = new List<Issue>();
                issues_ =
                    rm.GetObjectList<Issue>(new NameValueCollection
                    {
                        {"include", "relations,journals"},
                        {"status_id", "6"},
                        {"project_id", redProject.Id.ToString()},
                        {"fixed_version_id" , selectedVersion.Id.ToString()}
                    });

                while (issues_.Count > 0)
                {
                    rejectedIssues = rejectedIssues.Concat(issues_).ToList();
                    offset += 25;
                    issues_ =
                        rm.GetObjectList<Issue>(new NameValueCollection
                        {
                            {"include", "relations,journals"},
                            {"status_id", "6"},
                            {"offset", offset.ToString()},
                            {"project_id", redProject.Id.ToString()},
                            {"fixed_version_id" , selectedVersion.Id.ToString()}
                        });
                }

                var issuesWithoutSupport = issues;
                _supportIssuesCount = 0;
                issues = new List<Issue>();
                foreach (var iss in issuesWithoutSupport)
                {
                    if (iss.Tracker != null && iss.Tracker.Id != 3)
                        issues.Add(iss);
                    else _supportIssuesCount++;
                }
            });
        }
        static List<Issue> _rejectedIssues;

        public static Task<string> BuildReport(List<ProjectInfo> projectInfo, ProjectInfo core)
        {
            return Task.Run(() =>
            {
                JsonRelease release = new JsonRelease();

                if (!Directory.Exists("Report")) Directory.CreateDirectory("Report");
                var dir = Directory.GetCurrentDirectory().ParentDirectory().ParentDirectory();
                string report = File.ReadAllText(dir + "/Report/report.html");

                if (File.Exists("Report/report.html")) File.Delete("Report/report.html");
                StreamWriter writer = new StreamWriter("Report/report.html");

                report = report.Replace("{release_cycle}", RedmineVersion);
                release.ReleaseCycle = RedmineVersion;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var w = new ReportArgsWindow();
                    w.ShowDialog();
                    report = report.Replace("{title}", w.ReportArgs.Title);
                    release.ReleaseTitle = w.ReportArgs.Title;

                    report = report.Replace("{general_feeling}", w.ReportArgs.Feeling);
                    release.ReleaseGeneralFeeling = w.ReportArgs.Feeling;

                    if (w.ReportArgs.Notes != null)
                    {
                        report = report.Replace("{notes}", w.ReportArgs.Notes.Replace(Environment.NewLine, "<br/>"));
                    }
                    release.ReleaseNotes = w.ReportArgs.Notes;

                    report = report.Replace("{server_version}", w.ReportArgs.ServerVersion);
                    release.ServerVersion = w.ReportArgs.ServerVersion;
                });

                release.TicketsDesign = designTickets.Count;
                report = report.Replace("{bugs}",
                    issues.Count(iss => iss.Tracker != null && iss.Tracker.Id == 1).ToString());
                release.BugsResolved = issues.Count(iss => iss.Tracker != null && iss.Tracker.Id == 1);

                report = report.Replace("{features}",
                    issues.Count(iss => iss.Tracker != null && iss.Tracker.Id == 2).ToString());
                release.FeaturesAdded = issues.Count(iss => iss.Tracker != null && iss.Tracker.Id == 2);

                report = report.Replace("{refactors}",
                    issues.Count(iss => iss.Tracker != null && iss.Tracker.Id == 9).ToString());
                release.TicketsForRefactoring = issues.Count(iss => iss.Tracker != null && iss.Tracker.Id == 9);


                report = report.Replace("{rejected_tickets}", _rejectedIssues.Count.ToString());
                release.TicketsRejected = _rejectedIssues.Count;


                report = report.Replace("{support_tickets}", _supportIssuesCount.ToString());
                release.SupportTicketsHandled = _supportIssuesCount;

                var aliasBuilder = new StringBuilder();

                var issuesSorted = issues.Where(i => i.CustomFields.Any(
                    cf => cf.Name == "Package")).GroupBy(i => i.CustomFields.First(
                    cf => cf.Name == "Package").Values.Join("<br/>")).ToList();

                release.ReleaseIssuesPerProject = new List<JsonProjectIssues>();

                string project;

                foreach (var iss in issuesSorted)
                {
                    JsonProjectIssues pi = new JsonProjectIssues();
                    pi.ProjectInfo = new List<JsonIssuesInfo>();
                    project = iss.Key;

                    pi.ProjectName = project;
                    aliasBuilder.Append(String.Format(@"<tr><td>{0}</td><td><table id=""box-table-a"">
				            <thead>
					            <tr>
						            <th>Changelog</th>
						            <th>ID</th>
					            </tr>
				            </thead>
				            <tbody>", project));

                    foreach (var issue in iss.ToList())
                    {
                        var prinf = new JsonIssuesInfo();
                        var changelog = "";
                        try
                        {
                            changelog =
                                issue.CustomFields.First(cf => cf.Name == "Change log message").Values[0].ToString();
                        }
                        catch
                        {
                        }
                        prinf.ChangelogName = changelog;
                        prinf.TicketId = issue.Id;
                        prinf.Title = issue.Subject;
                        prinf.Tracker = issue.Tracker.Name;
                        pi.ProjectInfo.Add(prinf);
                        Regex regex = new Regex(@"#([0-9])\w+");
                        var match = regex.Matches(changelog);
                        if (match.Count > 0)
                        {
                            foreach (var m in match)
                            {
                                changelog = changelog.Replace(m.ToString(),
                                    "<a target='_blank' href='http://redmine.dotbydot.eu/issues/" +
                                    m.ToString().Replace("#", "") + "'>" + m.ToString() + "</a>");
                            }
                        }

                        aliasBuilder.Append(String.Format(@"<tr><td title=""{0}"">(<strong>{1}</strong>) {2}</td><td>{3}</td></tr>",
                            issue.Subject,
                            issue.Tracker.Name,
                            changelog,
                            "<a target='_blank' href=\"http://redmine.dotbydot.eu/issues/" + issue.Id + "\">" +
                            issue.Id + "</a>"
                            ));

                    }
                    aliasBuilder.Append("</tbody></table></td></tr>");
                    release.ReleaseIssuesPerProject.Add(pi);
                }
                report = report.Replace("{tickets}", aliasBuilder.ToString());

                release.ReleaseRejectedIssues = new List<JsonRejectedIssuesInfo>();
                aliasBuilder = new StringBuilder();
                if (rejectedIssues.Count > 0)
                {
                    foreach (var issue in rejectedIssues)
                    {
                        JsonRejectedIssuesInfo pi = new JsonRejectedIssuesInfo();
                        var changelog = "";
                        try
                        {
                            changelog =
                                issue.CustomFields.First(cf => cf.Name == "Reason for Rejecting").Values[0].ToString();
                        }
                        catch
                        {
                        }
                        pi.Reason = changelog;
                        pi.TicketId = issue.Id;
                        pi.Title = issue.Subject;
                        Regex regex = new Regex(@"#([0-9])\w+");
                        var match = regex.Matches(changelog);
                        if (match.Count > 0)
                        {
                            foreach (var m in match)
                            {
                                changelog = changelog.Replace(m.ToString(),
                                    "<a target='_blank' href='http://redmine.dotbydot.eu/issues/" +
                                    m.ToString().Replace("#", "") + "'>" + m.ToString() + "</a>");
                            }
                        }

                        aliasBuilder.Append(
                            String.Format(@"<tr><td>(<strong>{0}</strong>) {1}</td><td>{2}</td><td>{3}</td></tr>",
                                issue.Tracker.Name,
                                issue.Subject,
                                changelog,
                                "<a target='_blank' href=\"http://redmine.dotbydot.eu/issues/" + issue.Id + "\">" +
                                issue.Id + "</a>"
                                ));
                        release.ReleaseRejectedIssues.Add(pi);
                    }


                    report = report.Replace("{rejected-tickets}", aliasBuilder.ToString());
                }
                report = report.Replace("{core_version}", core.SuggestedVersion);
                release.CoreClientVersion = core.SuggestedVersion;
                release.UserPluginsVersions = new List<string>();
                if (projectInfo.Any(p => p.Update))
                {
                    aliasBuilder.Clear();
                    var updated = projectInfo.Where(p => p.Update).ToList();
                    List<string> plversions = new List<string>();
                    updated.ForEach(p =>
                    {
                        plversions.Add(string.Format("{0} {1}", p.Name, p.SuggestedVersion));
                        aliasBuilder.Append(string.Format("<li>{0} {1}</li>", p.Name, p.SuggestedVersion));
                    });
                    release.UserPluginsVersions = plversions;
                    report = report.Replace("{plugin_versions}", aliasBuilder.ToString());
                }
                writer.Write(report);
                writer.Close();
                var json = JsonSerializer.SerializeToString(release);
                using (var sw = new StreamWriter("Report/report.json"))
                {
                    sw.Write(json);
                }
                foreach (var file in Directory.GetFiles(dir + "/Report"))
                {
                    if (file.EndsWith(".html")) continue;
                    try
                    {
                        File.Copy(file, "Report/" + Path.GetFileName(file), true);
                    }
                    catch
                    {
                    }
                }
                return json;
            });
        }
    }
}
