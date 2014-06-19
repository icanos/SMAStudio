using SMAStudio.Services;
using SMAStudio.SMAWebService;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SMAStudio.Commands
{
    public class LoadCommand : ICommand
    {
        private IWorkspaceViewModel _dataContext;
        private IComponentsViewModel _componentsViewModel;

        public LoadCommand()
        {
            _dataContext = Core.Resolve<IWorkspaceViewModel>();
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
            var obj = (RoutedPropertyChangedEventArgs<object>)parameter;

            if (obj.NewValue is RunbookViewModel)
            {
                AsyncService.Execute(ThreadPriority.Normal, delegate()
                {
                    var runbook = (RunbookViewModel)obj.NewValue;
                    runbook.Content = runbook.GetContent();

                    _dataContext.OpenDocument(runbook);
                });

                _dataContext.WindowTitle = ((RunbookViewModel)obj.NewValue).RunbookName;
            }
            else if (obj.NewValue is VariableViewModel || obj.NewValue is CredentialViewModel)
            {
                AsyncService.Execute(ThreadPriority.Normal, delegate()
                {
                    var variable = (IDocumentViewModel)obj.NewValue;

                    _dataContext.OpenDocument(variable);
                });

                if (obj.NewValue is VariableViewModel)
                    _dataContext.WindowTitle = ((VariableViewModel)obj.NewValue).Name;
                else
                    _dataContext.WindowTitle = ((CredentialViewModel)obj.NewValue).Name;
            }
        }
    }
}
