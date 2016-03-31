using Caliburn.Micro;
using Gemini.Framework.Services;
using SMAStudiovNext.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using SMAStudiovNext.Modules.DialogAddNewItem.Windows;
using SMAStudiovNext.Modules.PartEnvironmentExplorer.ViewModels;
using SMAStudiovNext.Modules.WindowRunbook.ViewModels;

namespace SMAStudiovNext.Modules.Shell.Commands
{
    public class NewRunbookCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var shell = IoC.Get<IShell>();
            
            var dialog = new UIAddNewItemDialog();
            dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            var showDialog = dialog.ShowDialog();
            if (showDialog == null || !(bool) showDialog)
                return;

            var reader = new StreamReader(dialog.SelectedTemplate.Path);
            reader.ReadLine(); // Skip the first line since that contains the DESCRIPTION
            var runbookContent = reader.ReadToEnd();

            reader.Close();
                
            var context = IoC.Get<EnvironmentExplorerViewModel>().GetCurrentContext();
            if (context != null)
            {
                var viewModel = default(RunbookViewModel);

                var check = context.Runbooks.FirstOrDefault(r => r.Title.Equals(dialog.CreatedName, StringComparison.InvariantCultureIgnoreCase));
                if (check == null)
                {
                    switch (context.ContextType)
                    {
                        case Core.ContextType.SMA:
                            var runbook = new SMA.Runbook {RunbookName = dialog.CreatedName};

                            viewModel = new RunbookViewModel(new RunbookModelProxy(runbook, context));
                            viewModel.AddSnippet(runbookContent);
                            break;
                        case Core.ContextType.Azure:
                            var azureRunbook = new Vendor.Azure.Runbook {RunbookName = dialog.CreatedName};

                            viewModel = new RunbookViewModel(new RunbookModelProxy(azureRunbook, context));
                            viewModel.AddSnippet(runbookContent);
                            break;
                    }

                    if (viewModel != null)
                        shell.OpenDocument(viewModel);
                }
                else
                {
                    MessageBox.Show("A runbook with the same name already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Unable to determine context.");
            }
        }
    }
}
