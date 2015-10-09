using SMAStudio.Util;
using SMAStudio.SMAWebService;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SMAStudio.Logging;
using SMAStudio.Services;

namespace SMAStudio.Commands
{
    public class CheckInCommand : ICommand
    {
        private IApiService _api;
        private IRunbookService _runbookService;

        public CheckInCommand()
        {
            _api = Core.Resolve<IApiService>();
            _runbookService = Core.Resolve<IRunbookService>();
        }

        public bool CanExecute(object parameter)
        {
            if (parameter == null)
                return false;

            if (!(parameter is RunbookViewModel))
                return false;
            
            var document = ((IDocumentViewModel)parameter);

            // If this is a new runbook, it won't be able to be checked in before
            // it has been saved.
            if (String.IsNullOrEmpty(((RunbookViewModel)document).RunbookName))
                return false;

            if (!document.CheckedOut)
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

            var runbookViewModel = (RunbookViewModel)parameter;

            // Save the runbook and check in
            _runbookService.CheckIn(runbookViewModel);

            runbookViewModel.UnsavedChanges = false;
        }
    }
}
