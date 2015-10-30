using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation.Language;
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

namespace SMAStudio
{
    /// <summary>
    /// Interaction logic for PrepareRunWindow.xaml
    /// </summary>
    public partial class PrepareRunWindow : Window
    {
        private RunbookViewModel _runbookViewModel;

        public PrepareRunWindow(RunbookViewModel runbookViewModel)
        {
            _runbookViewModel = runbookViewModel;

            InitializeComponent();

            DataContext = this;
            Inputs = new ObservableCollection<UIInputParameter>();

            Loaded += PrepareRunWindowLoaded;
        }

        private void PrepareRunWindowLoaded(object sender, RoutedEventArgs e)
        {
            var parameters = _runbookViewModel.GetParameters();

            foreach (var param in parameters)
                Inputs.Add(param);
            //Inputs.Addparameters.OrderBy(i => i.Name).ToObservableCollection();
        }

        public ObservableCollection<UIInputParameter> Inputs
        {
            get;
            set;
        }

        /*private string ConvertToNiceName(string parameterName)
        {
            if (parameterName == null)
                return string.Empty;

            if (parameterName.Length == 0)
                return string.Empty;

            parameterName = parameterName.Replace("$", "");
            parameterName = char.ToUpper(parameterName[0]) + parameterName.Substring(1);

            return parameterName;
        }
        */
        private void Run_Click(object sender, RoutedEventArgs e)
        {
            bool hasErrors = false;

            foreach (var input in Inputs)
            {
                if (input.Required && String.IsNullOrEmpty(input.Value))
                {
                    hasErrors = true;
                    MessageBox.Show("Missing required parameter: " + input.Name, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                }
            }

            if (hasErrors)
                return;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class UIInputParameter
    {
        public string Name { get; set; }

        public string Command { get; set; }

        public string Value { get; set; }

        public bool IsArray { get; set; }

        public string TypeName { get; set; }

        public bool Required { get; set; }

        public string DescribingName
        {
            get { return TypeName + ": " + Name + (Required ? " (required)" : ""); }
        }
    }
}
