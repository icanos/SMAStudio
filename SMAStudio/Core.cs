using Microsoft.Practices.Unity;
using SMAStudio.Commands;
using SMAStudio.Editor.Parsing;
using SMAStudio.Logging;
using SMAStudio.Services;
using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            _container.RegisterType<ICommand, RefreshCommand>("Refresh");
            _container.RegisterType<ICommand, RevertCommand>("Revert");
            _container.RegisterType<ICommand, RunCommand>("Run");
            _container.RegisterType<ICommand, SaveCommand>("Save");
            _container.RegisterType<ICommand, StopCommand>("Stop");
            _container.RegisterType<ICommand, TestCommand>("Test");
        }

        public static T Resolve<T>()
        {
            return _container.Resolve<T>();
        }

        public static T Resolve<T>(string name)
        {
            return _container.Resolve<T>(name);
        }

        public static string Version
        {
            get { return "0.1.5"; }
        }
    }
}
