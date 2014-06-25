using SMAStudio.Util;
using SMAStudio.SMAWebService;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SMAStudio.Services;
using SMAStudio.Settings;
using SMAStudio.Logging;

namespace SMAStudio.Commands
{
    public class CheckOutCommand : ICommand
    {
        private IApiService _api;
        private IRunbookService _runbookService;

        public CheckOutCommand()
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

            var runbook = ((RunbookViewModel)parameter);

            if (!runbook.Runbook.PublishedRunbookVersionID.HasValue)
                return false;

            if (runbook.CheckedOut)
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

            _runbookService.CheckOut((RunbookViewModel)parameter, SilentCheckOut);
        }

        /// <summary>
        /// Set to true if the checkout should be silent
        /// </summary>
        public bool SilentCheckOut
        {
            private get;
            set;
        }
    }
}
