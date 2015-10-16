using SMAStudio.ViewModels;
using System.Windows;

namespace SMAStudio
{
    /// <summary>
    /// Interaction logic for ExecutionWindow.xaml
    /// </summary>
    public partial class ExecutionWindow : Window
    {
        private RunbookViewModel _runbookViewModel;

        public ExecutionWindow(RunbookViewModel runbookViewModel)
        {
            _runbookViewModel = runbookViewModel;

            InitializeComponent();

            DataContext = new ExecutionViewModel(runbookViewModel);
            Width = (SystemParameters.PrimaryScreenWidth / 4) * 3;   // 3/4 of the screen
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var executionViewModel = (ExecutionViewModel)DataContext;
            executionViewModel.ClosingWindow();
        }
    }
}
