using SMAStudio.SMAWebService;
using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudio.Commands
{
    public class RunCommand : ICommand
    {
        private ApiService _api;

        public RunCommand()
        {
            _api = new ApiService();
        }

        public bool CanExecute(object parameter)
        {
            if (parameter is RunbookViewModel)
                return true;

            return false;
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

            if (!(parameter is RunbookViewModel))
                return;

            var runbook = (RunbookViewModel)parameter;
            var window = new PrepareRunWindow(runbook);
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            if (!(bool)window.ShowDialog())
                return;

            List<NameValuePair> parameters = null;

            if (window.Inputs.Count > 0)
            {
                parameters = new List<NameValuePair>();

                foreach (var param in window.Inputs)
                {
                    parameters.Add(new NameValuePair
                        {
                            Name = param.Command,
                            Value = JsonConverter.ToJson(param.Value)
                        });
                }
            }

            _api.Current.AttachTo("Runbooks", runbook.Runbook);
            _api.Current.UpdateObject(runbook.Runbook);

            Guid? jobGuid = new Guid?(runbook.Runbook.StartRunbook(_api.Current, parameters));
            runbook.JobID = (Guid)jobGuid;

            var executionWindow = new ExecutionWindow(runbook);
            executionWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            executionWindow.Show();
        }
    }
}
