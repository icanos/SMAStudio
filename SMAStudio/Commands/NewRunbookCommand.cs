using SMAStudio.Services;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudio.Commands
{
    public class NewRunbookCommand : ICommand
    {
        private IRunbookService _runbookService;

        public NewRunbookCommand()
        {
            _runbookService = Core.Resolve<IRunbookService>();
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
            var addNewItemDialog = new AddNewItemDialog();
            addNewItemDialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            if ((bool)addNewItemDialog.ShowDialog())
            {
                TextReader reader = new StreamReader(addNewItemDialog.SelectedTemplate.Path);
                reader.ReadLine(); // Skip the first line since that contains the DESCRIPTION
                string runbookContent = reader.ReadToEnd();

                reader.Close();

                _runbookService.Create(addNewItemDialog.CreatedName, runbookContent);
            }
        }
    }
}
