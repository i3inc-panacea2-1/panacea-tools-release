using Panacea.Tools.Release.Models;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Redmine.Net.Api;
using Redmine.Net.Api.Types;
using Mono.Cecil;

namespace Panacea.Tools.Release
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
        }
        public static Window Instance;

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Instance = this;
            MessageHelper.Message += UpdateStatus;

            try
            {
                await Utils.DiscoverSolution();
                //await Builder.Build(sinfo);
                //var projectHelper = new ProjectHelper(sinfo);
                //await projectHelper.GetPluginProjectInfo();
                //await projectHelper.GetCoreProjectInfo();

                //var translations = await TranslatorParser.GetTranslations(Path.GetDirectoryName(sinfo.SolutionName));

                //if (translations.ContainsKey("core"))
                //{
                //    foreach (var str in translations["core"].Where(str => !projectHelper.CoreProjectInfo.Translations.Contains(str)))
                //    {
                //        projectHelper.CoreProjectInfo.Translations.Add(str);
                //        projectHelper.CoreProjectInfo.ManifestChanged = true;
                //    }
                //}
                //var trans = new Dictionary<string, List<String>>();
                //trans.Add("core", projectHelper.CoreProjectInfo.Translations);
                //var projectsWithoutTranslations = new List<string>();
                //foreach (var proj in projectHelper.PluginsProjectInfo)
                //{
                //    if (translations.ContainsKey(proj.Name))
                //    {
                //        foreach (var str in translations[proj.Name].Where(str => !proj.Translations.Contains(str)))
                //        {
                //            proj.Translations.Add(str);
                //            proj.ManifestChanged = true;
                //        }
                //        trans.Add(proj.Name, proj.Translations);
                //        proj.Translations = proj.Translations.Where(c => !c.Contains("β") && !c.Contains("€")).ToList();
                //    }
                //    else projectsWithoutTranslations.Add(proj.Name);
                //}

                //using (var writer = new StreamWriter("translations.txt")) writer.Write(JsonSerializer.SerializeToString(trans));
                //if (projectsWithoutTranslations.Count > 0)
                //    MessageBox.Show(String.Format("No translations found for plugins: {0}",
                //        String.Join(Environment.NewLine, projectsWithoutTranslations)));

                //await projectHelper.CalculateLocalCoreFiles();
                //await projectHelper.CalculateLocalFiles();

                //await RedmineHelper.GetChangesFromRedmine();
                //await RedmineHelper.AssignTicketsToProject(projectHelper.CoreProjectInfo);
                //foreach (var proj in projectHelper.PluginsProjectInfo)
                //{
                //    await RedmineHelper.AssignTicketsToProject(proj);
                //}

                //await projectHelper.BuildSignatures();
                //await projectHelper.BuildCoreSignatures();
                //await projectHelper.CheckCompatibilityOfPreviousPluginsWithNewCore();
                //await projectHelper.CheckCompatibilityOfNewPluginsWithNewCore();

                var overview = new ProjectsOverview()
                {
                    Applications = Utils.Applications,
                    Modules = Utils.Modules
                };
                overview.Show();

                this.Close();
            }
            catch (Exception ex)
            {
                Utils.Panic(ex.Message + Environment.NewLine + ex.StackTrace);
            }


        }

        void UpdateStatus(object sender, string text)
        {
            try
            {
                Dispatcher.Invoke(() => status.Text = text);
            }
            catch { }
        }
        void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.Write(e.Data);
        }

        
    }
}
