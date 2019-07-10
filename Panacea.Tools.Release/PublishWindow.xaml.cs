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
              
            }
            catch(Exception ex)
            {
                Utils.Panic(ex.Message);
            }
        }
    }
}
