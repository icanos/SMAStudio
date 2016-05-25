using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Caliburn.Micro;
using Gemini.Framework.Services;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.WindowConnection.ViewModels;
using SMAStudiovNext.Modules.WindowCredential.ViewModels;
using SMAStudiovNext.Modules.WindowExecutionResult.ViewModels;
using SMAStudiovNext.Modules.WindowRunbook.ViewModels;
using SMAStudiovNext.Modules.WindowSchedule.ViewModels;
using SMAStudiovNext.Modules.WindowVariable.ViewModels;
using SMAStudiovNext.Utils;
using SMAStudiovNext.Services;
using SMAStudiovNext.Core;
using SMAStudiovNext.Modules.Shell.ViewModels;
using System.Windows;
using Newtonsoft.Json;

namespace SMAStudiovNext.Modules.PartEnvironmentExplorer.Commands
{
    public class LoadCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (parameter is JobModelProxy) // From History View
                return true;

            if (!(parameter is ResourceContainer))
                return false;

            var viewItem = (ResourceContainer)parameter;

            if (viewItem.Tag == null)
                return false;

            if (viewItem.Tag is RunbookModelProxy 
                || viewItem.Tag is VariableModelProxy 
                || viewItem.Tag is ScheduleModelProxy 
                || viewItem.Tag is CredentialModelProxy 
                || viewItem.Tag is ConnectionModelProxy
                || viewItem.Tag is IBackendContext)
                return true;

            return false;
        }

        public void Execute(object parameter)
        {
            var shell = IoC.Get<IShell>();

            if (parameter is ResourceContainer)
            {
                // This command has been called from the Environment Explorer tool
                var viewItem = (ResourceContainer)parameter;

                if (viewItem.Tag is IBackendContext)
                {
                    LongRunningOperation.Start();

                    shell.StatusBar.Items[0].Message = "Loading data from " + (viewItem.Tag as IBackendContext).Name + "...";
                    Task.Run(() => {
                        try
                        {
                            (viewItem.Tag as IBackendContext).Start();
                        }
                        catch (AggregateException ex)
                        {
                            Caliburn.Micro.Execute.OnUIThread(() =>
                            {
                                if (ex.InnerException != null)
                                    MessageBox.Show(ex.InnerException.Message, "Error");
                                else
                                    MessageBox.Show(ex.Message, "Error");

                                LongRunningOperation.Stop();
                            });
                        }
                    });

                    return;
                }

                if (!(viewItem.Tag is ModelProxyBase))
                    return;

                LongRunningOperation.Start();
                shell.StatusBar.Items[0].Message = "Loading " + viewItem.Title + "...";

                Task.Run(delegate ()
                {
                    var viewModel = (ModelProxyBase)viewItem.Tag;

                    if (viewItem.Tag is RunbookModelProxy)
                        shell.OpenDocument(viewModel.GetViewModel<RunbookViewModel>());
                    else if (viewItem.Tag is VariableModelProxy)
                        shell.OpenDocument(viewModel.GetViewModel<VariableViewModel>());
                    else if (viewItem.Tag is CredentialModelProxy)
                        shell.OpenDocument(viewModel.GetViewModel<CredentialViewModel>());
                    else if (viewItem.Tag is ScheduleModelProxy)
                        shell.OpenDocument(viewModel.GetViewModel<ScheduleViewModel>());
                    else if (viewItem.Tag is ConnectionModelProxy)
                        shell.OpenDocument(viewModel.GetViewModel<ConnectionViewModel>());

                    shell.StatusBar.Items[0].Message = "";

                    CommandManager.InvalidateRequerySuggested();
                });
            }
            else if (parameter is JobModelProxy)
            {
                // This command has been called from our Job History view
                var jobProxy = (JobModelProxy)parameter;
                
                shell.OpenDocument(new ExecutionResultViewModel(jobProxy.BoundRunbookViewModel, jobProxy.JobID, false));
            }
        }
    }
}
