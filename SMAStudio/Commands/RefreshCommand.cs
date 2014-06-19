using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudio.Commands
{
    public class RefreshCommand : ICommand
    {
        private IComponentsViewModel _componentsViewModel;

        public RefreshCommand()
        {
            _componentsViewModel = Core.Resolve<IComponentsViewModel>();
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
            _componentsViewModel.Load(true /* force download */);
        }
    }
}
