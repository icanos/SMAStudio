/* Copyright 2014 Marcus Westin

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.*/

using SMAStudio.Editor;
using SMAStudio.Services;
using SMAStudio.Settings;
using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

        private IAutoSaveService _autoSaveManager;

        public MainWindow()
        {
            _instance = this;

            InitializeComponent();

            Core.Log.InfoFormat("\r\n\r\nStarted new intance of SMA Studio 2014 v " + Core.Version);

            Delegates();
            if (!ConfigureSettingsManager())
            {
                Close();
                return;
            }

            Core.Start();

            // Verify that we have connectivity against SMA before continuing
            var apiService = new ApiService();
            if (!apiService.TestConnectivity())
            {
                MessageBox.Show("Invalid Service Management Automation URL and/or credentials. Please verify connectivity and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            ConfigureDataContexts();
            ConfigureAutoSaver();

            tabDocuments.SelectedIndex = 0;

            ContextMenuConverter.DocumentReferenceContextMenu = (ContextMenu)Resources["DocumentReferenceContextMenu"];

            Core.Log.DebugFormat("Successfully initialized SMA Studio 2014.");            
        }

        private void ConfigureDataContexts()
        {
            Core.Log.DebugFormat("Configuring data bindings...");

            errorList.DataContext = Core.Resolve<IErrorListViewModel>();
            DataContext = Core.Resolve<IWorkspaceViewModel>();
            explorerList.DataContext = Core.Resolve<IComponentsViewModel>();
            Toolbar.DataContext = Core.Resolve<IToolbarViewModel>();
        }

        private void ConfigureAutoSaver()
        {
            Core.Log.DebugFormat("Starting the auto save manager");

            _autoSaveManager = Core.Resolve<IAutoSaveService>();
            _autoSaveManager.Start();
        }

        private bool ConfigureSettingsManager()
        {
            if (!SettingsManager.Current.Settings.IsConfigured)
            {
                Core.Log.InfoFormat("No settings.xml has been configured. Running first time wizard.");

                var window = new WelcomeDialog();
                window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                if (!(bool)window.ShowDialog())
                {
                    Core.Log.InfoFormat("User cancelled out of Welcome Wizard");
                    return false;
                }
            }

            return true;
        }

        private void Delegates()
        {
            Loaded += delegate(object sender, RoutedEventArgs e)
            {
                ((IWorkspaceViewModel)DataContext).Initialize();
                ((IComponentsViewModel)explorerList.DataContext).Initialize();
            };

            Closing += delegate(object sender, CancelEventArgs e)
            {
                if (DataContext == null)
                    return;

                Core.Log.DebugFormat("Close application event received");

                SettingsManager.Current.Dispose();
                Core.Log.DebugFormat("Stopped Settings Manager");

                foreach (var document in ((WorkspaceViewModel)DataContext).Documents)
                {
                    if (document.UnsavedChanges)
                    {
                        Core.Log.DebugFormat("Detected unsaved changes in a document");

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

                Core.Log.DebugFormat("Stopping auto save manager and async service.");

                ((IDisposable)_autoSaveManager).Dispose();
                AsyncService.Stop();
            };
        }

        private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
                treeViewItem.Focus();            

            if (treeViewItem != null)
            {
                if (!(treeViewItem.Header is RunbookViewModel))
                    return;

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
            if ((RunbookVersionViewModel)((MenuItem)e.OriginalSource).Tag == null)
                return;

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

        private void FindReplaceClicked(object sender, RoutedEventArgs e)
        {

        }
    }
}
