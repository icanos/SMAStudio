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

namespace SMAStudiovNext.Modules.PropertyGrid.Views
{
    /// <summary>
    /// Interaction logic for PropertyGridViw.xaml
    /// </summary>
    public partial class PropertyGridView : UserControl
    {
        public PropertyGridView()
        {
            // The following line simply forces Visual Studio to copy the
            // WPF Toolkit DLL to the output folder.
            _propertyGrid = null;

            InitializeComponent();
        }
    }
}
