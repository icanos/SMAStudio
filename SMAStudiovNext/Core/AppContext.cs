using Microsoft.Practices.Unity;
using SMAStudiovNext.Commands;
using SMAStudiovNext.Modules.EnvironmentExplorer.Commands;
using SMAStudiovNext.Modules.Runbook.Snippets;
using SMAStudiovNext.Modules.Shell.Commands;
using SMAStudiovNext.Services;
using System.Windows.Input;

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

            _container.RegisterType<ICommand, LoadCommand>("LoadCommand");
            _container.RegisterType<ICommand, HistoryCommand>("HistoryCommand");
            _container.RegisterType<ICommand, DeleteCommand>("DeleteCommand");
            _container.RegisterType<ICommand, NewCredentialCommand>("NewCredentialCommand");
            _container.RegisterType<ICommand, NewVariableCommand>("NewVariableCommand");
            _container.RegisterType<ICommand, NewScheduleCommand>("NewScheduleCommand");
            _container.RegisterType<ICommand, NewRunbookCommand>("NewRunbookCommand");
            _container.RegisterType<ICommand, GoToDefinitionCommand>("GoToDefinitionCommand");
            _container.RegisterType<ICommand, GenerateCertificateCommand>("GenerateCertificateCommand");
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
            get { return "0.5.0"; }
        }
    }
}
