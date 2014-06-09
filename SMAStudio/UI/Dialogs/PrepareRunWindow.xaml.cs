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
            Token[] tokens;
            ParseError[] parseErrors;

            var scriptBlock = Parser.ParseInput(_runbookViewModel.Content, out tokens, out parseErrors);

            if (scriptBlock.EndBlock == null || scriptBlock.EndBlock.Statements.Count == 0)
            {
                MessageBox.Show("Your script is broken and cannot be run. Please fix any errors.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var functionBlock = (FunctionDefinitionAst)scriptBlock.EndBlock.Statements[0];

            if (functionBlock.Body.ParamBlock != null)
            {
                foreach (var param in functionBlock.Body.ParamBlock.Parameters)
                {
                    var input = new UIInputParameter
                    {
                        Name = ConvertToNiceName(param.Name.Extent.Text),
                        Command = param.Name.Extent.Text
                    };

                    Inputs.Add(input);
                }

                Inputs = Inputs.OrderBy(i => i.Name).ToObservableCollection();
            }
        }

        public ObservableCollection<UIInputParameter> Inputs
        {
            get;
            set;
        }

        private string ConvertToNiceName(string parameterName)
        {
            parameterName = parameterName.Replace("$", "");
            parameterName = char.ToUpper(parameterName[0]) + parameterName.Substring(1);

            return parameterName;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }

    public class UIInputParameter
    {
        public string Name { get; set; }

        public string Command { get; set; }

        public string Value { get; set; }
    }
}
