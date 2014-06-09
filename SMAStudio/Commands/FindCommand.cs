using SMAStudio.Editor;
using SMAStudio.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SMAStudio.Commands
{
    /// <summary>
    /// TODO: This command needs to be rewritten to incorporate the MVVM pattern
    /// </summary>
    public class FindCommand : ICommand
    {
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
            var textEditor = FindVisualChildByName<MvvmTextEditor>(MainWindow.Instance.Tabs, "textEditor");

            FindReplaceDialog findDialog = new FindReplaceDialog(textEditor);
            findDialog.Topmost = true;
            findDialog.Show();
        }

        private T FindVisualChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            T child = default(T);
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var ch = VisualTreeHelper.GetChild(parent, i);
                child = ch as T;
                if (child != null && child.Name == name)
                    break;
                else
                    child = FindVisualChildByName<T>(ch, name);

                if (child != null) break;
            }
            return child;
        }
    }
}
