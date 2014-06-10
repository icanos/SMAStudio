using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using SMAStudio.Commands;
using SMAStudio.Editor;
using SMAStudio.Logging;
using SMAStudio.Services;
using SMAStudio.Settings;
using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Xml;

namespace SMAStudio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static MainWindow _instance = null;
        public static MainWindow Instance
        {
            get
            {
                return _instance;
            }
        }

        public const string SMA_STUDIO_NAME = " - SMA Studio 2014";

        private AutoSaveService _autoSaveManager;

        public MainWindow()
        {
            _instance = this;

            InitializeComponent();

            Closing += delegate(object sender, CancelEventArgs e)
            {
                if (DataContext == null)
                    return;

                SettingsManager.Current.Dispose();

                foreach (var document in ((WorkspaceViewModel)DataContext).Documents)
                {
                    if (document.UnsavedChanges)
                    {
                        //hasUnsavedChanges = true;
                        string message = "Save {0} \"{1}\"?";

                        if (document is RunbookViewModel)
                            message = String.Format(message, "runbook", ((RunbookViewModel)document).RunbookName);
                        else if (document is VariableViewModel)
                            message = String.Format(message, "variable", ((VariableViewModel)document).Name);
                        else if (document is CredentialViewModel)
                            message = String.Format(message, "credential", ((CredentialViewModel)document).Name);

                        var result = MessageBox.Show(message, "Save", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            // Save!
                            ((WorkspaceViewModel)DataContext).SaveCommand.Execute(document);
                        }
                        else if (result == MessageBoxResult.No)
                        {
                            // discard
                        }
                        else if (result == MessageBoxResult.Cancel)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }

                _autoSaveManager.Dispose();
                AsyncService.Stop();
            };

            tabDocuments.SelectedIndex = 0;

            if (!SettingsManager.Current.Settings.IsConfigured)
            {
                var window = new WelcomeDialog();
                window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                if (!(bool)window.ShowDialog())
                {
                    Close();
                    return;
                }
            }

            // Data binding
            errorList.DataContext = new ErrorListViewModel();
            DataContext = new WorkspaceViewModel((ErrorListViewModel)errorList.DataContext);
            explorerList.DataContext = new ComponentsViewModel((WorkspaceViewModel)DataContext);

            ((WorkspaceViewModel)DataContext).Components = (ComponentsViewModel)explorerList.DataContext;

            Toolbar.DataContext = new ToolbarViewModel((ComponentsViewModel)explorerList.DataContext);

            _autoSaveManager = new AutoSaveService((WorkspaceViewModel)DataContext);
            _autoSaveManager.Start();

            var log = new log4netLoggingService();
            log.DebugFormat("Running SMA Studio 2014");
        }

        private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
            treeViewItem.Focus();

            if (!(treeViewItem.Header is RunbookViewModel))
                return;

            if (treeViewItem != null)
            {
                SelectedItem = treeViewItem.Header;

                var runbook = (RunbookViewModel)SelectedItem;

                btnCompare.Items.Clear();

                if (!runbook.CheckedOut)
                    btnCompare.IsEnabled = false;
                else
                    btnCompare.IsEnabled = true;

                if (runbook.LoadedVersions)
                {
                    foreach (var version in runbook.Versions)
                    {
                        MenuItem menuItem = new MenuItem();
                        menuItem.Header = "revision " + version.RunbookVersion.VersionNumber;
                        menuItem.Tag = version;

                        btnCompare.Items.Add(menuItem);
                    }
                }
                else
                {
                    btnCompare.Items.Add(new MenuItem() { Header = "Loading..." });
                }
            }
        }

        static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        public object SelectedItem
        {
            get;
            set;
        }

        public TabControl Tabs
        {
            get { return tabDocuments; }
        }

        private void CompareClicked(object sender, RoutedEventArgs e)
        {
            CompareWindow compareWindow = new CompareWindow((RunbookViewModel)SelectedItem, (RunbookVersionViewModel)((MenuItem)e.OriginalSource).Tag);
            compareWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            compareWindow.Show();
        }

        private void textEditor_TextChanged(object sender, EventArgs e)
        {
            IDocumentViewModel documentViewModel = (IDocumentViewModel)tabDocuments.SelectedItem;

            if (documentViewModel == null)
                return;

            documentViewModel.TextChanged(sender, e);

            documentViewModel.LastTimeKeyDown = DateTime.Now;
        }

        /// <summary>
        /// Called when the MvvmTextEditor content changes, in order for code completion to work correctly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TextEntered(object sender, TextCompositionEventArgs e)
        {
            var workspace = (WorkspaceViewModel)DataContext;
            workspace.EditorTextEntered(sender, e);
        }

        /// <summary>
        /// Called when the MvvmTextEditor content changes, in order for code completion to work correctly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TextEntering(object sender, TextCompositionEventArgs e)
        {
            var workspace = (WorkspaceViewModel)DataContext;
            workspace.EditorTextEntering(sender, e);
        }

        private void FindReplaceClicked(object sender, RoutedEventArgs e)
        {

        }
    }
}
