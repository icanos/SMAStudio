using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Modules.ErrorList;
using Gemini.Modules.Output;
using SMAStudiovNext.Agents;
using SMAStudiovNext.Core;
using SMAStudiovNext.Core.Tracing;
using SMAStudiovNext.Icons;
using SMAStudiovNext.Modules.WindowConsole.ViewModels;
using SMAStudiovNext.Services;
using SMAStudiovNext.Themes;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SMAStudiovNext.Exceptions;
using SMAStudiovNext.Modules.PartEnvironmentExplorer.ViewModels;
using SMAStudiovNext.Utils;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Parser;

namespace SMAStudiovNext.Modules.Startup
{
    [Export(typeof(IModule))]
    public class Module : ModuleBase
    {
        private readonly IErrorList _errorList;
        private readonly IOutput _output;

        private readonly IList<IAgent> _agents;
        private readonly IList<IBackendContext> _backendContexts;

        private readonly object _lock = new object();
        
        [ImportingConstructor]
        public Module()
        {
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");

            _agents = new List<IAgent>();
            _backendContexts = new List<IBackendContext>();

            _errorList = IoC.Get<IErrorList>();
            _output = IoC.Get<IOutput>();
        }

        public override void Initialize()
        {
            Application.Current.MainWindow.Icon = new BitmapImage(new Uri("pack://application:,,," + IconsDescription.SMAStudio32, UriKind.RelativeOrAbsolute));
            AppContext.Start();
            CertificateManager.Configure();

            // Force software rendering!
            RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

            // Enable tracing
            TracingAdapter.SetWriter(new TracingInAppWriter());
            TracingAdapter.IsEnabled = false;

            MainWindow.Title = "Automation Studio";

            Shell.ShowFloatingWindowsInTaskbar = true;
            Shell.ToolBars.Visible = true;
            Shell.StatusBar.AddItem("Starting Automation Studio v. " + AppContext.Version + "...", new System.Windows.GridLength(1, System.Windows.GridUnitType.Star));
            Shell.ActiveDocumentChanged += (sender, e) => RefreshInspector();
            
            try
            {
                if (!File.Exists(Path.Combine(AppHelper.CachePath, "ApplicationState.bin")))
                {
                    var envExplorer = Shell.Tools.FirstOrDefault(x => x.ContentId == "SMAStudio.EnvironmentExplorer");

                    if (envExplorer == null)
                        Shell.ShowTool(new EnvironmentExplorerViewModel());

                    Shell.ShowTool(_errorList);
                }
            }
            catch (Exception)
            {

            }

            // Load settings from the settings.xml file
            var settingsService = AppContext.Resolve<ISettingsService>();
            settingsService.Load();

            // Load themes after the settings has been initialized
            var themeManager = AppContext.Resolve<IThemeManager>();
            themeManager.LoadThemes();

            Shell.AttemptingDeactivation += (sender, e) =>
            {
                settingsService.Save();

                //AsyncExecution.Stop();

                foreach (var agent in _agents)
                    agent.Stop();
            };
            
            // Retrieve all agents
            var agentTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IAgent))).ToList();

            // Start all agents
            foreach (var agentType in agentTypes)
            {
                var agent = (IAgent)Activator.CreateInstance(agentType);
                agent.Start();

                _agents.Add(agent);
                AppContext.Register<IAgent>(agent, agentType.Name);
            }

            // Initialize all connections
            if (SettingsService.CurrentSettings != null)
            {
                //AsyncExecution.Run(System.Threading.ThreadPriority.Normal, () =>
                Task.Run(() =>
                {
                    for (int i = 0; i < SettingsService.CurrentSettings.Connections.Count; i++)
                    {
                        StartConnection(SettingsService.CurrentSettings.Connections[i]);
                    }
                });
            }

            if (SettingsService.CurrentSettings.EnableCodeAnalysis)
            {
                AnalyzerService.Start();
            }

            _output.AppendLine("Started Automation Studio");

            Shell.StatusBar.Items[0].Message = "";
        }

        public override void PostInitialize()
        {
            base.PostInitialize();

            //var consoleView = new ConsoleViewModel();
            //Shell.OpenDocument(consoleView);
        }

        public void StartConnection(BackendConnection connection)
        {
            var contextType = ContextType.SMA;

            if (connection.IsAzure)
                contextType = ContextType.Azure;
            else if (connection.IsAzureRM)
                contextType = ContextType.AzureRM;

            var backend = new BackendContext(contextType, connection);
            backend.OnLoaded += OnBackendReady;
            backend.Start();

            _backendContexts.Add(backend);

            var environment = IoC.Get<EnvironmentExplorerViewModel>();
            Execute.OnUIThread(() =>
            {
                lock (_lock)
                {
                    environment.Items.Add(backend.GetStructure());
                }
            });
        }

        public IList<IBackendContext> GetContexts()
        {
            return _backendContexts;
        }

        private void OnBackendReady(object sender, ContextUpdatedEventArgs e)
        {
            var environment = IoC.Get<EnvironmentExplorerViewModel>();

            if (environment != null)
                environment.OnBackendReady(sender, e);

            bool allContextsReady = true;
            foreach (var context in GetContexts())
            {
                if (!context.IsReady)
                {
                    allContextsReady = false;
                    break;
                }
            }

            if (allContextsReady)
                AppContext.Resolve<IStatusManager>().SetText("");
        }

        private void RefreshInspector()
        {

        }

        public IList<IAgent> Agents
        {
            get { return _agents; }
        }
    }
}
