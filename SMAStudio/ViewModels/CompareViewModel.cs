using SMAStudio.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudio.ViewModels
{
    public class CompareViewModel
    {
        private CompareWindow _compareWindow;
        private ICommand _compareCommand;
        
        public CompareViewModel(CompareWindow compareWindow)
        {
            _compareWindow = compareWindow;

            _compareCommand = new CompareCommand(compareWindow);
        }

        public ICommand CompareCommand
        {
            get { return _compareCommand; }
        }
    }
}
