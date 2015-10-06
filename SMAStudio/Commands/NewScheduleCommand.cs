using SMAStudio.Services;
using SMAStudio.Services.SMA;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudio.Commands
{
    public class NewScheduleCommand : ICommand
    {
        private IScheduleService _scheduleService;

        public NewScheduleCommand()
        {
            _scheduleService = Core.Resolve<IScheduleService>();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            _scheduleService.Create();
        }
    }
}
