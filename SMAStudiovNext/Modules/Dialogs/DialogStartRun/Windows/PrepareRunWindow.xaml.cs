using System;
using System.Collections.ObjectModel;
using System.Windows;
using ICSharpCode.AvalonEdit.CodeCompletion;
using SMAStudiovNext.Modules.WindowRunbook.ViewModels;
using SMAStudiovNext.Core.Editor.Completion;

namespace SMAStudiovNext.Modules.DialogStartRun.Windows
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
            Inputs = new ObservableCollection<ICompletionData>();

            Loaded += PrepareRunWindowLoaded;
        }

        private void PrepareRunWindowLoaded(object sender, RoutedEventArgs e)
        {
            var parameters = _runbookViewModel.GetParameters(null, true);

            foreach (var param in parameters)
                Inputs.Add(param);
        }

        public ObservableCollection<ICompletionData> Inputs
        {
            get;
            set;
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            var hasErrors = false;

            foreach (var input in Inputs)
            {
                var parameter = (ParameterCompletionData)input;

                if (!parameter.IsRequired || !String.IsNullOrEmpty(parameter.Text))
                    continue;

                hasErrors = true;
                MessageBox.Show("Missing required parameter: " + input.Text, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                break;
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
}
