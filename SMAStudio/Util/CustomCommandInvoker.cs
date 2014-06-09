using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace SMAStudio.Util
{
    public sealed class CustomCommandInvoker : TriggerAction<DependencyObject>
    {
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(ICommand), typeof(CustomCommandInvoker), null);

        public ICommand Command
        {
            get
            {
                return (ICommand)this.GetValue(CommandProperty);
            }
            set
            {
                this.SetValue(CommandProperty, value);
            }
        }
        protected override void Invoke(object parameter)
        {

            if (this.AssociatedObject != null)
            {
                ICommand command = Command;
                if ((command != null) && command.CanExecute(parameter))
                {
                    command.Execute(parameter);
                }
            }
        }
    }
}
