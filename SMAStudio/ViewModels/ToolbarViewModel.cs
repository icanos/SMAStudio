using SMAStudio.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudio.ViewModels
{
    public class ToolbarViewModel
    {
        private ICommand _saveCommand;
        private ICommand _checkInCommand;
        private ICommand _checkOutCommand;
        private ICommand _runCommand;
        private ICommand _stopCommand;
        private ICommand _refreshCommand;
        private ICommand _revertCommand;

        public ToolbarViewModel(ComponentsViewModel componentsViewModel)
        {
            _saveCommand = new SaveCommand(componentsViewModel);
            _checkInCommand = new CheckInCommand();
            _checkOutCommand = new CheckOutCommand();
            _runCommand = new RunCommand();
            _stopCommand = new StopCommand();
            _refreshCommand = new RefreshCommand(componentsViewModel);
            _revertCommand = new RevertCommand();
        }

        public ICommand SaveCommand
        {
            get { return _saveCommand; }
        }

        public ICommand CheckInCommand
        {
            get { return _checkInCommand; }
        }

        public ICommand CheckOutCommand
        {
            get { return _checkOutCommand; }
        }

        public ICommand RunCommand
        {
            get { return _runCommand; }
        }

        public ICommand StopCommand
        {
            get { return _stopCommand; }
        }

        public ICommand RefreshCommand
        {
            get { return _refreshCommand; }
        }

        public ICommand RevertCommand
        {
            get { return _revertCommand; }
        }
    }
}
