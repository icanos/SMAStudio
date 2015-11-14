﻿using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Modules.ErrorList;
using Gemini.Modules.Output;
using SMAStudiovNext.Agents;
using SMAStudiovNext.Core;
using SMAStudiovNext.Icons;
using SMAStudiovNext.Modules.EnvironmentExplorer.ViewModels;
using SMAStudiovNext.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SMAStudiovNext.Modules.Startup
{
    [Export(typeof(IModule))]
    public class Module : ModuleBase
    {
        private readonly IErrorList _errorList;
        private readonly IOutput _output;

        private readonly IList<IAgent> _agents;
        private readonly IList<IBackendContext> _backendContexts;
        
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
            Console.WriteLine("Initializing SMA Studio");

            Application.Current.MainWindow.Icon = new BitmapImage(new Uri("pack://application:,,," + IconsDescription.SMAStudio32, UriKind.RelativeOrAbsolute));
            AppContext.Start();
            CertificateManager.Configure();

            MainWindow.Title = "SMA Studio 2015";

            Shell.ShowFloatingWindowsInTaskbar = true;
            Shell.ToolBars.Visible = true;
            Shell.StatusBar.AddItem("Starting SMA Studio...", new System.Windows.GridLength(1, System.Windows.GridUnitType.Star));
            Shell.ActiveDocumentChanged += (sender, e) => RefreshInspector();
            
            try
            {
                if (!File.Exists(Path.Combine(AppHelper.ApplicationPath, "ApplicationState.bin")))
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

            Shell.AttemptingDeactivation += (sender, e) =>
            {
                settingsService.Save();

                AsyncExecution.Stop();

                foreach (var agent in _agents)
                    agent.Stop();
            };

            /*AsyncExecution.Run(ThreadPriority.Normal, () =>
            {
                var smaService = AppContext.Resolve<IBackendService>("SMA");
                smaService.Load();

                var azureService = AppContext.Resolve<IBackendService>("Azure");
                azureService.Load();
            });*/

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
            AsyncExecution.Run(System.Threading.ThreadPriority.Normal, () =>
            {
                foreach (var connection in SettingsService.CurrentSettings.Connections)
                {
                    StartConnection(connection);
                }
            });

            _output.AppendLine("Started SMA Studio");

            Shell.StatusBar.Items[0].Message = "";
        }

        public void StartConnection(BackendConnection connection)
        {
            var backend = new BackendContext(connection.IsAzure ? ContextType.Azure : ContextType.SMA, connection);
            backend.OnLoaded += OnBackendReady;
            backend.Start();

            _backendContexts.Add(backend);
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