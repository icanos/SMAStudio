using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SMAStudio.Services
{
    public class BaseService
    {
        internal void NotifyConnectionError()
        {
            if (App.Current == null)
                return;

            App.Current.Dispatcher.Invoke(delegate()
            {
                MessageBox.Show("Unable to connect to the SMA server. Please verify the connectivity and try again.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
    }
}
