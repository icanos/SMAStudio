using Gemini.Framework;
using Gemini.Modules.Shell.Views;
using SMAStudiovNext.Modules.Shell.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SMAStudiovNext.Modules.Shell.Views
{
    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    public partial class ShellView : IShellView, IAutomationStudioShellView
    {
        public ShellView()
        {
            InitializeComponent();
        }

        public ProgressBar Progress
        {
            get { return prgWaiting; }
        }

        public void LoadLayout(Stream stream, Action<ITool> addToolCallback, Action<IDocument> addDocumentCallback,
                               Dictionary<string, ILayoutItem> itemsState)
        {
            LayoutUtility.LoadLayout(Manager, stream, addDocumentCallback, addToolCallback, itemsState);
        }

        public void SaveLayout(Stream stream)
        {
            LayoutUtility.SaveLayout(Manager, stream);
        }

        private void OnManagerLayoutUpdated(object sender, EventArgs e)
        {
            UpdateFloatingWindows();
        }

        public void UpdateFloatingWindows()
        {
            var mainWindow = Window.GetWindow(this);
            var mainWindowIcon = (mainWindow != null) ? mainWindow.Icon : null;
            var showFloatingWindowsInTaskbar = ((ShellViewModel)DataContext).ShowFloatingWindowsInTaskbar;
            foreach (var window in Manager.FloatingWindows)
            {
                window.Icon = mainWindowIcon;
                window.ShowInTaskbar = showFloatingWindowsInTaskbar;
            }
        }
    }
}
