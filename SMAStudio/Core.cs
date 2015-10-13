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

using Microsoft.Practices.Unity;
using SMAStudio.Commands;
using SMAStudio.Editor.Parsing;
using SMAStudio.Logging;
using SMAStudio.Services;
using SMAStudio.Services.SMA;
using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SMAStudio
{
    public class Core
    {
        private static IUnityContainer _container = null;

        private static ILoggingService _instance = null;
        public static ILoggingService Log
        {
            get
            {
                if (_instance == null)
                    _instance = new log4netLoggingService();

                return _instance;
            }
        }

        public static void Start()
        {
            _container = new UnityContainer();

            _container.RegisterInstance<IParserService>(new ParserService());

            // View models
            _container.RegisterInstance<IErrorListViewModel>(new ErrorListViewModel());
            _container.RegisterInstance<IWorkspaceViewModel>(new WorkspaceViewModel(_container.Resolve<IErrorListViewModel>(), _container.Resolve<IParserService>()));
            _container.RegisterInstance<IComponentsViewModel>(new ComponentsViewModel(_container.Resolve<IWorkspaceViewModel>()));
            _container.RegisterInstance<IToolbarViewModel>(new ToolbarViewModel());

            // Services
            _container.RegisterInstance<IApiService>(new ApiService());
            _container.RegisterInstance<IRunbookService>(new RunbookService());
            _container.RegisterInstance<IVariableService>(new VariableService());
            _container.RegisterInstance<ICredentialService>(new CredentialService());
            _container.RegisterInstance<IScheduleService>(new ScheduleService());
            _container.RegisterInstance<IAutoSaveService>(new AutoSaveService(_container.Resolve<IWorkspaceViewModel>(), _container.Resolve<IApiService>()));

            // Commands
            _container.RegisterType<ICommand, AboutCommand>("About");
            _container.RegisterType<ICommand, CheckInCommand>("CheckIn");
            _container.RegisterType<ICommand, CheckOutCommand>("CheckOut");
            _container.RegisterType<ICommand, CloseAllCommand>("CloseAll");
            _container.RegisterType<ICommand, CloseCommand>("Close");
            _container.RegisterType<ICommand, DeleteCommand>("Delete");
            _container.RegisterType<ICommand, ExitCommand>("Exit");
            _container.RegisterType<ICommand, FindCommand>("Find");
            _container.RegisterType<ICommand, GoDefinitionCommand>("GoDefinition");
            _container.RegisterType<ICommand, LoadCommand>("Load");
            _container.RegisterType<ICommand, NewCredentialCommand>("NewCredential");
            _container.RegisterType<ICommand, NewRunbookCommand>("NewRunbook");
            _container.RegisterType<ICommand, NewVariableCommand>("NewVariable");
            _container.RegisterType<ICommand, NewScheduleCommand>("NewSchedule");
            _container.RegisterType<ICommand, RefreshCommand>("Refresh");
            _container.RegisterType<ICommand, RevertCommand>("Revert");
            _container.RegisterType<ICommand, RunCommand>("Run");
            _container.RegisterType<ICommand, ResumeCommand>("Resume");
            _container.RegisterType<ICommand, SaveCommand>("Save");
            _container.RegisterType<ICommand, StopCommand>("Stop");
            _container.RegisterType<ICommand, TestCommand>("Test");

            VerifyVersion();

            Application.Current.DispatcherUnhandledException += UnhandledExceptionHandler;
        }

        private static void UnhandledExceptionHandler(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error("Unhandled exception.", e.Exception);
            MessageBox.Show("Oh no, SMA Studio just crashed, please restart and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            e.Handled = false;
        }

        public static T Resolve<T>()
        {
            return _container.Resolve<T>();
        }

        public static T Resolve<T>(string name)
        {
            return _container.Resolve<T>(name);
        }

        private static void VerifyVersion()
        {
            AsyncService.Execute(ThreadPriority.Normal, delegate()
            {
                //
                try
                {
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("http://www.sekurbit.se/version.txt");

                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    TextReader reader = new StreamReader(response.GetResponseStream());

                    string version = reader.ReadToEnd();

                    reader.Close();

                    if (!version.Equals(Core.Version))
                    {
                        if (App.Current == null)
                            return;

                        App.Current.Dispatcher.Invoke(delegate()
                        {
                            /*var result = MessageBox.Show("A new version of SMA Studio 2014 is available.\r\nDo you want to download it now?", "New version", MessageBoxButton.YesNo, MessageBoxImage.Information);
                            if (result == MessageBoxResult.Yes)
                            {
                                // Open a browser
                                Process.Start("http://www.sekurbit.se/");
                            }*/
                        });
                    }
                }
                catch (WebException)
                {
                    Core.Log.WarningFormat("Unable to check version. Continuing as normal.");
                }
            });
        }

        public static string Version
        {
            get { return "0.3.4"; }
        }
    }
}
