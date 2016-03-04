using SMAStudiovNext.Models;
using SMAStudiovNext.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core
{
    public delegate void OnContextUpdatedDelegate(object sender, ContextUpdatedEventArgs e);

    public class ContextUpdatedEventArgs : EventArgs
    {
        public ContextUpdatedEventArgs(IBackendContext context)
        {
            Context = context;
        }

        public IBackendContext Context { get; set; }
    }

    public interface IBackendContext
    {
        /// <summary>
        /// Event called when the SMA server has been contacted and all objects been enumerated
        /// </summary>
        event OnContextUpdatedDelegate OnLoaded;

        void Start();

        void AddToRunbooks(RunbookModelProxy runbook);

        void AddToCredentials(CredentialModelProxy credential);

        void AddToVariables(VariableModelProxy variable);

        void AddToSchedules(ScheduleModelProxy schedule);

        void AddToModules(ModuleModelProxy module);

        void AddToConnections(ConnectionModelProxy connection);

        void ParseTags();
        
        void SignalCompleted();

        /// <summary>
        /// Download content from a url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        string GetContent(string url);

        Task<string> GetContentAsync(string url);

        ResourceContainer GetStructure();

        ObservableCollection<ResourceContainer> Runbooks { get; set; }

        ObservableCollection<ResourceContainer> Variables { get; set; }

        ObservableCollection<ResourceContainer> Credentials { get; set; }

        ObservableCollection<ResourceContainer> Schedules { get; set; }

        ObservableCollection<ResourceContainer> Tags { get; set; }

        ObservableCollection<ResourceContainer> Modules { get; set; }

        ObservableCollection<ResourceContainer> Connections { get; set; }

        Guid ID { get; }

        ContextType ContextType { get; }
        
        IBackendService Service { get; }

        bool IsReady { get; set; }
    }
}
