using PluginPackager2.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
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
    /// Interaction logic for PublishWindow.xaml
    /// </summary>
    public partial class PublishWindow : Window
    {
        public PublishWindow()
        {
            InitializeComponent();
        }

        private ProjectHelper _projectHelper;
        public PublishWindow(ProjectHelper projectHelper):this()
        {
            _projectHelper = projectHelper;
        }

        private async void Main_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                FileUploader.UploadProgressChanged += (oo, progress) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        UploadProgress.Value = progress;
                    });
                };
                string path = Path.Combine(Directory.GetCurrentDirectory(), "exported");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                StatusText.Text = "Settings new versions";

                if (_projectHelper.CoreProjectInfo.Update)
                    await _projectHelper.CoreProjectInfo.SetAssemblyVersion();

                foreach (var info in _projectHelper.PluginsProjectInfo.Where(p => p.Update))
                {
                    await info.SetAssemblyVersion();
                }

                var inf = Utils.DiscoverSolution();
               
                StatusText.Text = "Building solution";

                await Builder.Build(inf);

                StatusText.Text = String.Format("Calculating file differences...");
                if (_projectHelper.CoreProjectInfo.Update)
                    await _projectHelper.CalculateLocalCoreFiles();
                await _projectHelper.CalculateLocalFiles();

                if (_projectHelper.CoreProjectInfo.Update)
                {
                    StatusText.Text = String.Format("Making package for {0}", _projectHelper.CoreProjectInfo.Name);
                    await _projectHelper.CoreProjectInfo.BuildDeltaZip(path);
                }

                foreach (var info in _projectHelper.PluginsProjectInfo.Where(p => p.Update))
                {
                    StatusText.Text = String.Format("Making package for {0}", info.Name);

                    await info.BuildDeltaZip(path);
                }
                Progress.Maximum = _projectHelper.PluginsProjectInfo.Count(p => p.Update);
                Progress.IsIndeterminate = false;

                if (_projectHelper.CoreProjectInfo.Update)
                {
                    UploadProgress.Visibility = Visibility.Visible;
                    Progress.Maximum++;
                    StatusText.Text = String.Format("Uploading {0}", _projectHelper.CoreProjectInfo.Name);
                    var res =
                        await
                            FileUploader.UploadFile(String.Format("{0}/{1}.zip", path, _projectHelper.CoreProjectInfo.Name),
                                ConfigurationManager.AppSettings["server"] + "admin/remote/robopost/manifest/");
                    if (!res.Success)
                    {
                        Utils.Panic("Fail for {0}: {1}", _projectHelper.CoreProjectInfo.Name, res.Message);
                        return;
                    }
                    UploadProgress.Visibility = Visibility.Hidden;
                }
                // await Task.Delay(3000);
                Progress.Value++;

                foreach (var info in _projectHelper.PluginsProjectInfo.Where(p => p.Update))
                {

                    StatusText.Text = String.Format("Uploading {0}", info.Name);
                    UploadProgress.Visibility = Visibility.Visible;
                    var result =
                        await
                            FileUploader.UploadFile(String.Format("{0}/{1}.zip", path, info.Name),
                                ConfigurationManager.AppSettings["server"] + "admin/remote/robopost/manifest/");
                    if (!result.Success)
                    {
                        Utils.Panic("Fail for {0}: {1}", info.Name, result.Message);
                        return;
                    }
                    UploadProgress.Visibility = Visibility.Hidden;
                    Progress.Value++;
                }
                StatusText.Text = "Done!";
                await Task.Delay(3000);
                Application.Current.Shutdown();
            }
            catch(Exception ex)
            {
                Utils.Panic(ex.Message);
            }
        }
    }
}
