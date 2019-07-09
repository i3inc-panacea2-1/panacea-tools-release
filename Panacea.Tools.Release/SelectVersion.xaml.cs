using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for SelectVersion.xaml
    /// </summary>
    public partial class SelectVersion : Window,INotifyPropertyChanged
    {
        public SelectVersion()
        {
            InitializeComponent();
        }

        public Redmine.Net.Api.Types.Version Version { get; set; }


        public List<Redmine.Net.Api.Types.Version> Versions { get; set; }

        public SelectVersion(List<Redmine.Net.Api.Types.Version> versions):this()
        {

            this.Versions = versions;
            OnPropertyChanged("Versions");
        }

        void OnPropertyChanged(string name)
        {
            if(PropertyChanged!=null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
