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
                MessageBox.Show("Your runbook is broken and it's possible that the runbook won't run. Please fix any errors.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var functionBlock = (FunctionDefinitionAst)scriptBlock.EndBlock.Statements[0];

            if (functionBlock.Body.ParamBlock != null)
            {
                if (functionBlock.Body.ParamBlock.Parameters == null)
                {
                    Core.Log.InfoFormat("Runbook contains ParamBlock but no Parameters.");
                    return;
                }

                foreach (var param in functionBlock.Body.ParamBlock.Parameters)
                {
                    try
                    {
                        var input = new UIInputParameter
                        {
                            Name = ConvertToNiceName(param.Name.Extent.Text),
                            Command = param.Name.Extent.Text
                        };

                        Inputs.Add(input);
                    }
                    catch (Exception ex)
                    {
                        Core.Log.Error("Unable to create a UIInputParameter for a runbook parameter.", ex);
                    }
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
            if (parameterName == null)
                return string.Empty;

            if (parameterName.Length == 0)
                return string.Empty;

            parameterName = parameterName.Replace("$", "");
            parameterName = char.ToUpper(parameterName[0]) + parameterName.Substring(1);

            return parameterName;
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
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
    }
}
