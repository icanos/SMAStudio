using Microsoft.Practices.Unity;
using SMAStudiovNext.Commands;
using SMAStudiovNext.Modules.Shell.Commands;
using SMAStudiovNext.Services;
using SMAStudiovNext.Themes;
using System.Windows.Input;
using SMAStudiovNext.Modules.PartEnvironmentExplorer.Commands;
using SMAStudiovNext.Core.Editor.Snippets;

namespace SMAStudiovNext.Core
{
    public class AppContext
    {
        private static IUnityContainer _container = null;

        public static void Start()
        {
            _container = new UnityContainer();
            
            _container.RegisterInstance<ISettingsService>(new SettingsService());
            _container.RegisterInstance<ISnippetsCollection>(new SnippetsCollection());
            _container.RegisterInstance<IStatusManager>(new StatusManager());
            _container.RegisterInstance<IThemeManager>(new ThemeManager());
            _container.RegisterInstance<IBackendContextManager>(new BackendContextManager(), new ContainerControlledLifetimeManager());

            _container.RegisterType<ICommand, LoadCommand>("LoadCommand");
            _container.RegisterType<ICommand, HistoryCommand>("HistoryCommand");
            _container.RegisterType<ICommand, DeleteCommand>("DeleteCommand");
            _container.RegisterType<ICommand, NewCredentialCommand>("NewCredentialCommand");
            _container.RegisterType<ICommand, NewVariableCommand>("NewVariableCommand");
            _container.RegisterType<ICommand, NewScheduleCommand>("NewScheduleCommand");
            _container.RegisterType<ICommand, NewRunbookCommand>("NewRunbookCommand");
            _container.RegisterType<ICommand, NewModuleCommand>("NewModuleCommand");
            _container.RegisterType<ICommand, NewConnectionObjectCommand>("NewConnectionObjectCommand");
            _container.RegisterType<ICommand, GoToDefinitionCommand>("GoToDefinitionCommand");
            _container.RegisterType<ICommand, GenerateCertificateCommand>("GenerateCertificateCommand");
            _container.RegisterType<ICommand, NewConnectionCommand>("NewConnectionCommand");
            _container.RegisterType<ICommand, RefreshCommand>("RefreshCommand");
            _container.RegisterType<ICommand, DocumentationCommand>("DocumentationCommand");
        }

        public static void Register<T>(T obj, string name)
        {
            _container.RegisterInstance<T>(name, obj);
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
            get { return "2.0.3"; }
        }
    }
}
