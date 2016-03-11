using Caliburn.Micro;
using SMAStudiovNext.Icons;
using SMAStudiovNext.Models;
using SMAStudiovNext.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        private readonly IList<string> _runbookNameCache;

        public event OnContextUpdatedDelegate OnLoaded;

        public BackendContext(ContextType backendType, BackendConnection connectionData)
        {
            _backendType = backendType;
            _backendConnection = connectionData;
            _statusManager = AppContext.Resolve<IStatusManager>();
            _runbookNameCache = new List<string>();
            
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
            Service.Load();

            _runbookNameCache.Clear();
            foreach (var rb in Runbooks)
                _runbookNameCache.Add((rb.Tag as RunbookModelProxy).RunbookName);
        }

        public ResourceContainer GetStructure()
        {
            return Service.GetStructure();
        }

        public void AddToRunbooks(RunbookModelProxy runbook)
        {
            try {
                Execute.OnUIThread(() =>
                {
                    Runbooks.Add(new ResourceContainer(runbook.RunbookName, runbook, IconsDescription.Runbook));

                    if (!_runbookNameCache.Contains(runbook.RunbookName))
                        _runbookNameCache.Add(runbook.RunbookName);
                });
            }
            catch (TaskCanceledException) { }
        }

        public void AddToModules(ModuleModelProxy module)
        {
            try
            {
                Execute.OnUIThread(() =>
                {
                    Modules.Add(new ResourceContainer(module.ModuleName, module, IconsDescription.Folder));
                });
            }
            catch (TaskCanceledException) { }
        }
        
        public void AddToCredentials(CredentialModelProxy credential)
        {
            try {
                Execute.OnUIThread(() =>
                {
                    Credentials.Add(new ResourceContainer(credential.Name, credential, IconsDescription.Credential));
                });
            }
            catch (TaskCanceledException) { }
        }

        public void AddToVariables(VariableModelProxy variable)
        {
            try {
                Execute.OnUIThread(() =>
                {
                    Variables.Add(new ResourceContainer(variable.Name, variable, IconsDescription.Variable));
                });
            }
            catch (TaskCanceledException) { }
        }

        public void AddToSchedules(ScheduleModelProxy schedule)
        {
            try {
                Execute.OnUIThread(() =>
                {
                    Schedules.Add(new ResourceContainer(schedule.Name, schedule, IconsDescription.Schedule));
                });
            }
            catch (TaskCanceledException) { }
        }

        public void AddToConnections(ConnectionModelProxy connection)
        {
            try
            {
                Execute.OnUIThread(() =>
                {
                    Connections.Add(new ResourceContainer(connection.Name, connection, IconsDescription.Connection));
                });
            }
            catch (TaskCanceledException)
            {
                // Silently continue
            }
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

                // MARK: Reworked this, I want this to work as follows, instead of creating a top level
                // folder for each tag, the tags should become nested for each tag added (parse from left to right).
                // So for eg. Order,Server => Runbooks / Order / Server / <runbook> instead of Runbooks / Order / <runbook> and Runbooks / Server / <runbook>

                var currentTags = Tags;

                //foreach (var tag in tags)
                for (var i = 0; i < tags.Length; i++)
                {
                    var tag = tags[i];

                    var fixedTagName = tag.Trim();
                    var count = currentTags.Count(x => x.Title == fixedTagName);
                    var tagResource = default(ResourceContainer);

                    if (count > 0)
                    {
                        // A tag already exists with this name, add the runbook
                        // to that instead of creating a new.
                        tagResource = currentTags.First(x => x.Title.Equals(fixedTagName, StringComparison.InvariantCultureIgnoreCase));

                        // Need to be executed on the UI thread since 'Tags' is an ObservableCollection.
                        // Only add the runbook to the deepest node in the tree
                        try {
                            if ((i + 1) == tags.Length)
                                Execute.OnUIThread(() => tagResource.Items.Add(runbook));
                        }
                        catch (TaskCanceledException) { }
                    }
                    else
                    {
                        var tagObj = new Tag(fixedTagName);
                        tagResource = new ResourceContainer(fixedTagName, tagObj, IconsDescription.Folder);
                        tagResource.Context = this;

                        // Only add the runbook to the deepest node in the tree
                        if ((i + 1) == tags.Length)
                            tagResource.Items.Add(runbook);

                        try {
                            Execute.OnUIThread(() => currentTags.Add(tagResource));
                        }
                        catch (TaskCanceledException) { }
                    }

                    tagResource.Items = tagResource.Items.OrderBy(item => item.Title).ToObservableCollection();
                    currentTags = tagResource.Items;
                }
            }

            try {
                Execute.OnUIThread(() =>
                {
                // We want unmatched at the bottom
                Tags = Tags.OrderBy(item => item.Title).ToObservableCollection();
                    Tags.Add(unmatchedTagMenuItem);

                    NotifyOfPropertyChange(() => Tags);
                });
            }
            catch (TaskCanceledException) { }
        }

        public string GetContent(string url)
        {
            return Service.GetContent(url);
        }

        public async Task<string> GetContentAsync(string url)
        {
            return await Service.GetContentAsync(url);
        }
        
        public void SignalCompleted()
        {
            if (OnLoaded != null)
                OnLoaded(this, new ContextUpdatedEventArgs(this));
        }

        public bool IsRunbook(string name)
        {
            return _runbookNameCache.Contains(name);
        }

        public ContextType ContextType { get { return _backendType; } }

        public Guid ID { get { return _backendConnection.Id; } }

        /// <summary>
        /// Contains all runbooks found in the backend
        /// </summary>
        private ObservableCollection<ResourceContainer> _runbooks;
        public ObservableCollection<ResourceContainer> Runbooks
        {
            get
            {
                return _runbooks;
            }
            set
            {
                _runbooks = value;
            }
        }

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
