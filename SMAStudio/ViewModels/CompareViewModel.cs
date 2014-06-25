using SMAStudio.Commands;
using SMAStudio.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudio.ViewModels
{
    public class CompareViewModel : ObservableObject
    {
        private CompareWindow _compareWindow;
        private ICommand _compareCommand;
        private ICommand _revertSpecificCommand;
        
        public CompareViewModel(CompareWindow compareWindow)
        {
            _compareWindow = compareWindow;

            _compareCommand = new CompareCommand(compareWindow);
        }

        #region Properties
        public ICommand CompareCommand
        {
            get { return _compareCommand; }
        }

        public ICommand RevertCommand
        {
            get { return _revertSpecificCommand; }
            set { _revertSpecificCommand = value; base.RaisePropertyChanged("RevertCommand"); }
        }
        #endregion
    }
}
