using Caliburn.Micro;
using Gemini.Framework.Services;
using SMAStudiovNext.Modules.Shell.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SMAStudiovNext.Core
{
    public static class LongRunningOperation
    {
        public static void Start()
        {
            var shell = IoC.Get<IShell>();

            Execute.OnUIThread(() =>
            {
                var customShell = (shell as IAutomationStudioShell);

                if (customShell.Progress == null)
                    return;

                customShell.Progress.Visibility = Visibility.Visible;
            });
        }

        public static void Stop()
        {
            var shell = IoC.Get<IShell>();

            Execute.OnUIThread(() =>
            {
                var customShell = (shell as IAutomationStudioShell);

                if (customShell.Progress == null)
                    return;

                customShell.Progress.Visibility = Visibility.Hidden;
            });
        }
    }
}
