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


        public List<LocalProject> Applications
        {
            get { return (List<LocalProject>)GetValue(ApplicationsProperty); }
            set { SetValue(ApplicationsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Applications.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ApplicationsProperty =
            DependencyProperty.Register("Applications", typeof(List<LocalProject>), typeof(ProjectsOverview), new PropertyMetadata(null));



        public List<LocalProject> Modules
        {
            get { return (List<LocalProject>)GetValue(ModulesProperty); }
            set { SetValue(ModulesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Modules.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModulesProperty =
            DependencyProperty.Register("Modules", typeof(List<LocalProject>), typeof(ProjectsOverview), new PropertyMetadata(null));


        public ProjectsOverview()
        {
            InitializeComponent();
        }

        private async void ButtonPublish_Click(object sender, RoutedEventArgs e)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "exported");


            var panacea = Applications.First(a => a.Name == "Panacea");
            if(panacea.Update)
            {
                foreach(var app in Applications)
                {
                    await app.Build();
                }
                await panacea.BuildDeltaZip(path);
            }
            
            var selected = Modules.Where(m => m.Update);
            
            foreach (var module in selected)
            {
                await module.Build();
                await module.BuildDeltaZip(path);
            }
            
        }
        private async void ButtonReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //var json = await RedmineHelper.BuildReport(ProjectHelper.PluginsProjectInfo, ProjectHelper.CoreProjectInfo);
                //var req = (HttpWebRequest) WebRequest.Create("http://internal.dotbydot.eu/store_release_report/");
                //req.Accept = "application/json";
                //req.ContentType = "application/x-www-form-urlencoded";
                //req.Method = WebRequestMethods.Http.Post;
                //var array = Encoding.UTF8.GetBytes("username=dbdinternal&password=dbdinternal01&releaseReport=" + System.Uri.EscapeDataString(json));
                //using (Stream dataStream = req.GetRequestStream())
                //{
                //    dataStream.Write(array, 0, array.Length);
                //}
                //var response = req.GetResponse();
                //MessageBox.Show(((HttpWebResponse) response).StatusDescription);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
       
        private void Button_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
           
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://redmine.dotbydot.eu/issues/" + ((Hyperlink)sender).Tag.ToString());
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
          
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
           
        }



    }
}
