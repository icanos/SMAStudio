using SMAStudio.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudio.ViewModels
{
    public class ToolbarViewModel : IToolbarViewModel
    {
        public ToolbarViewModel()
        {
            
        }

        #region Properties
        public ICommand SaveCommand
        {
            get { return Core.Resolve<ICommand>("Save"); }
        }

        public ICommand CheckInCommand
        {
            get { return Core.Resolve<ICommand>("CheckIn"); }
        }

        public ICommand CheckOutCommand
        {
            get { return Core.Resolve<ICommand>("CheckOut"); }
        }

        public ICommand RunCommand
        {
            get { return Core.Resolve<ICommand>("Run"); }
        }

        public ICommand ResumeCommand
        {
            get { return Core.Resolve<ICommand>("Resume"); }
        }

        public ICommand StopCommand
        {
            get { return Core.Resolve<ICommand>("Stop"); }
        }

        public ICommand RefreshCommand
        {
            get { return Core.Resolve<ICommand>("Refresh"); }
        }

        public ICommand RevertCommand
        {
            get { return Core.Resolve<ICommand>("Revert"); }
        }

        public ICommand TestCommand
        {
            get { return Core.Resolve<ICommand>("Test"); }
        }
        #endregion
    }
}
