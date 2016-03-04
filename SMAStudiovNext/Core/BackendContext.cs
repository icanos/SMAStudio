using Caliburn.Micro;
using SMAStudiovNext.Icons;
using SMAStudiovNext.Models;
using SMAStudiovNext.Services;
using SMAStudiovNext.SMA;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core
{
    public enum ContextType
    {
        SMA,
        Azure
    }

    /// <summary>
    /// Holds a connection to a backend, eg. a SMA environment or a Azure Automation account.
    /// </summary>
    public class BackendContext : PropertyChangedBase, IBackendContext
    {
        private readonly IBackendService _backendService;
        private readonly BackendConnection _backendConnection;
        private readonly ContextType _backendType;
        private readonly IStatusManager _statusManager;

        public event OnContextUpdatedDelegate OnLoaded;

        public BackendContext(ContextType backendType, BackendConnection connectionData)
        {
            _backendType = backendType;
            _backendConnection = connectionData;
            _statusManager = AppContext.Resolve<IStatusManager>();
            
            if (backendType == ContextType.SMA)
            {
                // SMA
                _backendService = new SmaService(this, connectionData);
            }
            else
            {
                // Azure
                _backendService = new AzureService(this, connectionData);
            }

            Runbooks = new ObservableCollection<ResourceContainer>();
            Credentials = new ObservableCollection<ResourceContainer>();
            Schedules = new ObservableCollection<ResourceContainer>();
            Variables = new ObservableCollection<ResourceContainer>();
            Tags = new ObservableCollection<ResourceContainer>();
            Modules = new ObservableCollection<ResourceContainer>();
            Connections = new ObservableCollection<ResourceContainer>();

            IsReady = false;
        }

        public void Start()
        {
            Runbooks.Clear();
            Credentials.Clear();
            Schedules.Clear();
            Variables.Clear();
            Modules.Clear();
            Connections.Clear();

            _statusManager.SetText("Loading data from " + _backendConnection.Name + "...");
            _backendService.Load();
        }

        public ResourceContainer GetStructure()
        {
            return _backendService.GetStructure();
        }

        public void AddToRunbooks(RunbookModelProxy runbook)
        {
            Execute.OnUIThread(() =>
            {
                Runbooks.Add(new ResourceContainer(runbook.RunbookName, runbook, IconsDescription.Runbook));
            });
        }

        public void AddToModules(ModuleModelProxy module)
        {
            Execute.OnUIThread(() =>
            {
                Modules.Add(new ResourceContainer(module.ModuleName, module, IconsDescription.Folder));
            });
        }
        
        public void AddToCredentials(CredentialModelProxy credential)
        {
            Execute.OnUIThread(() =>
            {
                Credentials.Add(new ResourceContainer(credential.Name, credential, IconsDescription.Credential));
            });
        }

        public void AddToVariables(VariableModelProxy variable)
        {
            Execute.OnUIThread(() =>
            {
                Variables.Add(new ResourceContainer(variable.Name, variable, IconsDescription.Variable));
            });
        }

        public void AddToSchedules(ScheduleModelProxy schedule)
        {
            Execute.OnUIThread(() =>
            {
                Schedules.Add(new ResourceContainer(schedule.Name, schedule, IconsDescription.Schedule));
            });
        }

        public void AddToConnections(ConnectionModelProxy connection)
        {
            Execute.OnUIThread(() =>
            {
                Connections.Add(new ResourceContainer(connection.Name, connection, IconsDescription.Connection));
            });
        }

        public void ParseTags()
        {
            var unmatchedTag = new Tag("(untagged)");
            var unmatchedTagMenuItem = new ResourceContainer("(untagged)", unmatchedTag, IconsDescription.Folder);
            unmatchedTagMenuItem.Context = this;

            foreach (var runbook in Runbooks)
            {
                var runbookModel = (RunbookModelProxy)runbook.Tag;
                if (runbookModel.Tags == null)
                {
                    Execute.OnUIThread(() =>
                    {
                        unmatchedTagMenuItem.Items.Add(runbook);
                    });
                    continue;
                }

                var tags = runbookModel.Tags.Split(',');
                if (tags.Length == 0)
                    continue;

                foreach (var tag in tags.OrderBy(t => t))
                {
                    var fixedTagName = tag.Trim();
                    var count = Tags.Count(x => x.Title == fixedTagName);
                    
                    if (count > 0)
                    {
                        var tagObj = default(ResourceContainer);
                        tagObj = Tags.First(x => x.Title == fixedTagName);

                        Execute.OnUIThread(() =>
                        {
                            tagObj.Items.Add(runbook);
                        });
                    }
                    else
                    {
                        var tagObj = new Tag(fixedTagName);
                        var menuItem = new ResourceContainer(fixedTagName, tagObj, IconsDescription.Folder);
                        menuItem.Context = this;
                        menuItem.Items.Add(runbook);

                        Execute.OnUIThread(() => { Tags.Add(menuItem); });
                    }
                }
            }

            Execute.OnUIThread(() =>
            {
                // We want unmatched at the bottom
                Tags.Add(unmatchedTagMenuItem);

                NotifyOfPropertyChange(() => Tags);
            });
        }

        public string GetContent(string url)
        {
            return _backendService.GetContent(url);
        }

        public async Task<string> GetContentAsync(string url)
        {
            return await _backendService.GetContentAsync(url);
        }
        
        public void SignalCompleted()
        {
            if (OnLoaded != null)
                OnLoaded(this, new ContextUpdatedEventArgs(this));
        }

        public ContextType ContextType { get { return _backendType; } }

        public Guid ID { get { return _backendConnection.Id; } }
        
        /// <summary>
        /// Contains all runbooks found in the backend
        /// </summary>
        public ObservableCollection<ResourceContainer> Runbooks { get; set; }

        /// <summary>
        /// Contains all variables found in the backend
        /// </summary>
        public ObservableCollection<ResourceContainer> Variables { get; set; }

        /// <summary>
        /// Contains all credentials found in the backend
        /// </summary>
        public ObservableCollection<ResourceContainer> Credentials { get; set; }

        /// <summary>
        /// Contains all schedules found in the backend
        /// </summary>
        public ObservableCollection<ResourceContainer> Schedules { get; set; }

        /// <summary>
        /// Contains a constructed tree of all runbooks based on tags
        /// </summary>
        public ObservableCollection<ResourceContainer> Tags { get; set; }

        /// <summary>
        /// Contains all modules found in the backend
        /// </summary>
        public ObservableCollection<ResourceContainer> Modules { get; set; }

        /// <summary>
        /// Contains all connections found in the backend
        /// </summary>
        public ObservableCollection<ResourceContainer> Connections { get; set; }

        public IBackendService Service
        {
            get { return _backendService; }
        }

        private bool _isReady = false;
        public bool IsReady
        {
            get { return _isReady; }
            set
            {
                if (_isReady.Equals(value))
                    return;

                _isReady = value;

                //if (_isReady)
                //    _statusManager.SetText("");
            }
        }
    }

    public class Tag : IEnvironmentExplorerItem
    {
        public Tag()
        {
            Runbooks = new List<ResourceContainer>();
        }

        public Tag(string name)
        {
            Name = name;
            Runbooks = new List<ResourceContainer>();
        }

        public string Name { get; set; }

        public IList<ResourceContainer> Runbooks { get; set; }
    }

    public class Folder : IEnvironmentExplorerItem
    {
        public Folder(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}
