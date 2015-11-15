using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Services;
using Gemini.Modules.Output;
using SMAStudiovNext.Core;
using SMAStudiovNext.Modules.EnvironmentExplorer.Views;
using SMAStudiovNext.Modules.Startup;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Windows.Input;

namespace SMAStudiovNext.Modules.EnvironmentExplorer.ViewModels
{
    [Export(typeof(EnvironmentExplorerViewModel))]
    public class EnvironmentExplorerViewModel : Tool
    {
        private readonly ObservableCollection<ResourceContainer> _items;
        private IEnvironmentExplorerView _view;

        #region ITool Properties
        public override PaneLocation PreferredLocation
        {
            get { return PaneLocation.Left; }
        }

        public ObservableCollection<ResourceContainer> Items
        {
            get { return _items; }
        }
        
        /// <summary>
        /// Declare new instead of override since we want to add a set method as well
        /// </summary>
        public new double PreferredWidth
        {
            get; set;
        }

        /// <summary>
        /// Declare new instead of override since we want to add a set method as well
        /// </summary>
        public new double PreferredHeight
        {
            get; set;
        }

        public override string DisplayName
        {
            get { return "Environment Explorer"; }
        }
        #endregion

        public EnvironmentExplorerViewModel()
        {
            _items = new ObservableCollection<ResourceContainer>();
        }

        protected override void OnViewLoaded(object view)
        {
            _view = (IEnvironmentExplorerView)view;

            /*var application = IoC.Get<IModule>();
            var contexts = (application as Module).GetContexts();

            foreach (var context in contexts)
            {
                Items.Add(context.GetStructure());
            }

            NotifyOfPropertyChange(() => Items);*/
        }

        public IBackendContext GetCurrentContext()
        {
            return _view.GetCurrentContext();
        }

        public void OnBackendReady(object sender, ContextUpdatedEventArgs e)
        {
            var context = e.Context;

            Execute.OnUIThread(() =>
            {
                var output = IoC.Get<IOutput>();
                output.AppendLine("All objects loaded!");

                NotifyOfPropertyChange(() => Items);
            });
        }

        public void Delete(ResourceContainer item)
        {
            Execute.OnUIThread(() =>
            {
                // This is kinda tricky since we have multiple levels of nested items
                var parentNode = Items.TreeFind(null, item);

                if (parentNode != null)
                    parentNode.Items.Remove(item);

                NotifyOfPropertyChange(() => Items);
            });
        }
        
        public ICommand LoadCommand
        {
            get { return AppContext.Resolve<ICommand>("LoadCommand"); }
        }

        public ICommand HistoryCommand
        {
            get { return AppContext.Resolve<ICommand>("HistoryCommand"); }
        }

        public ICommand DeleteCommand
        {
            get { return AppContext.Resolve<ICommand>("DeleteCommand"); }
        }

        public ICommand NewCredentialCommand
        {
            get { return AppContext.Resolve<ICommand>("NewCredentialCommand"); }
        }

        public ICommand NewVariableCommand
        {
            get { return AppContext.Resolve<ICommand>("NewVariableCommand"); }
        }

        public ICommand NewScheduleCommand
        {
            get { return AppContext.Resolve<ICommand>("NewScheduleCommand"); }
        }

        public ICommand NewRunbookCommand
        {
            get { return AppContext.Resolve<ICommand>("NewRunbookCommand"); }
        }
    }
}
