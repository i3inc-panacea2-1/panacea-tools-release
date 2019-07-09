using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Panacea.Tools.Release
{
    /// <summary>
    /// Interaction logic for ReportArgsWindow.xaml
    /// </summary>
    public partial class ReportArgsWindow : Window
    {
        public ReportArgsWindow()
        {
            InitializeComponent();
            ReportArgs = new ReportArgs();
            this.DataContext = ReportArgs;
        }
        public ReportArgs ReportArgs { get; set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class ReportArgs
    {
        public String Feeling { get; set; }
        public string Title { get; set; }
        public string Notes { get; set; }
        public string ServerVersion { get; set; }
        public string NextRelease { get; set; }
    }
}
