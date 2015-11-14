using Caliburn.Micro;
using Gemini.Framework.Services;
using SMAStudiovNext.Core;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.Runbook.ViewModels;
using System;
using System.Linq;
using System.Windows.Input;

namespace SMAStudiovNext.Commands
{
    public class GoToDefinitionCommand : ICommand
    {
        private readonly IBackendContext _backendContext;

        public GoToDefinitionCommand()
        {
            _backendContext = AppContext.Resolve<IBackendContext>();
        }

        private EventHandler _internalCanExecuteChanged;
        public event EventHandler CanExecuteChanged
        {
            add
            {
                _internalCanExecuteChanged += value;
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                _internalCanExecuteChanged -= value;
                CommandManager.RequerySuggested -= value;
            }
        }

        public bool CanExecute(object parameter)
        {
            if (!(parameter is RunbookViewModel))
                return false;

            var runbookViewModel = (RunbookViewModel)parameter;
            var context = runbookViewModel.GetCurrentContext();

            if (context == null)
                return false;
            
            // Check if we have placed the cursor in a keyword and if the word that the cursor is placed upon is a runbook or not
            if (context.Type != Language.ExpressionType.Keyword || (context.Type == Language.ExpressionType.Keyword && _backendContext.Runbooks.Count(r => ((RunbookModelProxy)r.Tag).RunbookName.Equals(context.Value, StringComparison.InvariantCultureIgnoreCase)) == 0))
                return false;

            return true;
        }

        public void Execute(object parameter)
        {
            if (!(parameter is RunbookViewModel))
                return;

            var runbookViewModel = (RunbookViewModel)parameter;
            var context = runbookViewModel.GetCurrentContext();

            if (context == null)
                return;

            var runbook = _backendContext.Runbooks.FirstOrDefault(r => ((RunbookModelProxy)r.Tag).RunbookName.Equals(context.Value, StringComparison.InvariantCultureIgnoreCase));

            if (runbook == null)
                return;

            // Get the view model of the referenced runbook and open that
            var viewModel = ((RunbookModelProxy)runbook.Tag).GetViewModel<RunbookViewModel>();

            var shell = IoC.Get<IShell>();
            shell.OpenDocument(viewModel);
        }

        /// <summary>
        /// This method is used to walk the delegate chain and well WPF that
        /// our command execution status has changed.
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            EventHandler eCanExecuteChanged = _internalCanExecuteChanged;

            if (eCanExecuteChanged != null)
                eCanExecuteChanged(this, EventArgs.Empty);
        }
    }
}
