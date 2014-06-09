using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SMAStudio.Commands
{
    public class StopCommand : ICommand
    {
        private ApiService _api;

        public StopCommand()
        {
            _api = new ApiService();
        }

        public bool CanExecute(object parameter)
        {
            if (parameter == null)
                return false;

            if (!(parameter is RunbookViewModel))
                return false;

            if (((RunbookViewModel)parameter).JobID == Guid.Empty)
                return false;

            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            if (parameter == null)
                return;

            if (MessageBox.Show("Are you sure you want to stop the execution?", "Stop execution", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var runbook = (RunbookViewModel)parameter;

                var job = _api.Current.Jobs.Where(j => j.JobID == runbook.JobID).First();
                job.Stop(_api.Current);

                runbook.JobID = Guid.Empty;
            }
        }
    }
}
