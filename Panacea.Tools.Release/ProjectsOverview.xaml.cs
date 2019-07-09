using MahApps.Metro.Controls;
using Panacea.Tools.Release.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Panacea.Tools.Release
{
    /// <summary>
    /// Interaction logic for ProjectsOverview.xaml
    /// </summary>
    public partial class ProjectsOverview : Window
    {
        public static readonly DependencyProperty ProjectHelperProperty =
            DependencyProperty.Register("ProjectHelper",
                typeof(ProjectHelper),
                typeof(ProjectsOverview),
                new FrameworkPropertyMetadata(null));
        public ProjectHelper ProjectHelper
        {
            get { return (ProjectHelper)GetValue(ProjectHelperProperty); }
            set { SetValue(ProjectHelperProperty, value); }
        }

        public static readonly DependencyProperty TicketCountProperty =
            DependencyProperty.Register("TicketCount",
                typeof(int),
                typeof(ProjectsOverview),
                new FrameworkPropertyMetadata(null));

        public int TicketCount
        {
            get { return (int)GetValue(TicketCountProperty); }
            set { SetValue(TicketCountProperty, value); }
        }

        public static readonly DependencyProperty ProjectsToBeUpdatedProperty =
            DependencyProperty.Register("ProjectsToBeUpdated",
                typeof(int),
                typeof(ProjectsOverview),
                new FrameworkPropertyMetadata(null));

        public int ProjectsToBeUpdated
        {
            get { return (int)GetValue(ProjectsToBeUpdatedProperty); }
            set { SetValue(ProjectsToBeUpdatedProperty, value); }
        }

        public ProjectsOverview()
        {
            InitializeComponent();
        }

        public ProjectsOverview(ProjectHelper helper)
            : this()
        {
            ProjectHelper = helper;
            TicketCount = ProjectHelper.PluginsProjectInfo.Sum(p => p.Bugs) + ProjectHelper.PluginsProjectInfo.Sum(p => p.Features) + ProjectHelper.CoreProjectInfo.Bugs + ProjectHelper.CoreProjectInfo.Features;
            ProjectsToBeUpdated = ProjectHelper.PluginsProjectInfo.Count(p => p.RequiresUpdate) + (ProjectHelper.CoreProjectInfo.RequiresUpdate ? 1 : 0);
        }



        private void ButtonPublish_Click(object sender, RoutedEventArgs e)
        {

            foreach (var proj in ProjectHelper.PluginsProjectInfo) proj.RedmineVersion = RedmineHelper.RedmineVersion;
            ProjectHelper.CoreProjectInfo.RedmineVersion = RedmineHelper.RedmineVersion;
            var w = new PublishWindow(ProjectHelper) { Owner = this };
            w.ShowDialog();
        }
        private async void ButtonReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var json = await RedmineHelper.BuildReport(ProjectHelper.PluginsProjectInfo, ProjectHelper.CoreProjectInfo);
                var req = (HttpWebRequest) WebRequest.Create("http://internal.dotbydot.eu/store_release_report/");
                req.Accept = "application/json";
                req.ContentType = "application/x-www-form-urlencoded";
                req.Method = WebRequestMethods.Http.Post;
                var array = Encoding.UTF8.GetBytes("username=dbdinternal&password=dbdinternal01&releaseReport=" + System.Uri.EscapeDataString(json));
                using (Stream dataStream = req.GetRequestStream())
                {
                    dataStream.Write(array, 0, array.Length);
                }
                var response = req.GetResponse();
                MessageBox.Show(((HttpWebResponse) response).StatusDescription);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
       
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (var info in ProjectHelper.PluginsProjectInfo)
            {
                info.Update = info.RequiresUpdate;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            foreach (var info in ProjectHelper.PluginsProjectInfo)
            {
                info.Update = false;
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://redmine.dotbydot.eu/issues/" + ((Hyperlink)sender).Tag.ToString());
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var version = Version.Parse(ProjectHelper.CoreProjectInfo.SuggestedVersion);
            ProjectHelper.CoreProjectInfo.SuggestedVersion = new Version(version.Major, version.Minor + 1, 0, 0).ToString();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            var version = Version.Parse(ProjectHelper.CoreProjectInfo.SuggestedVersion);
            if (version.Minor > 0)
                ProjectHelper.CoreProjectInfo.SuggestedVersion = new Version(version.Major, version.Minor - 1, version.Build, version.Revision).ToString();
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            var pi = ((Button)sender).Tag as ProjectInfo;
            var version = Version.Parse(pi.SuggestedVersion);
            
            pi.SuggestedVersion = new Version(version.Major, version.Minor + 1, 0, 0).ToString();
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            var pi = ((Button)sender).Tag as ProjectInfo;
            var version = Version.Parse(((ProjectInfo)((Button)sender).Tag).SuggestedVersion);
            if (version.Minor > 0)
                pi.SuggestedVersion = new Version(version.Major, version.Minor - 1, version.Build, version.Revision).ToString();
        }



    }
}
