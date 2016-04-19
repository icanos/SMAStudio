using SMAStudiovNext.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SMAStudiovNext.Modules.Dialogs.DialogConnectionManager
{
    public class ConnectionTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;

            if (item is BackendConnection)
            {
                var backendConnection = (BackendConnection)item;

                if (backendConnection.IsAzure)
                    return element.FindResource("Azure") as DataTemplate;

                return element.FindResource("SMA") as DataTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
