using Panacea.Tools.Release.Models;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Panacea.Tools.Release.Controls
{
    /// <summary>
    /// Interaction logic for ProjectList.xaml
    /// </summary>
    public partial class ProjectList : UserControl
    {


        public List<LocalProject> Projects
        {
            get { return (List<LocalProject>)GetValue(ProjectsProperty); }
            set { SetValue(ProjectsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Projects.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProjectsProperty =
            DependencyProperty.Register("Projects", typeof(List<LocalProject>), typeof(ProjectList), new PropertyMetadata(null));


        public ProjectList()
        {
            InitializeComponent();
        }
    }
}
