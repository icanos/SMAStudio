using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Modules.Output;
using SMAStudiovNext.Commands;
using SMAStudiovNext.Core;
using SMAStudiovNext.Modules.EnvironmentExplorer.Commands;
using SMAStudiovNext.Modules.EnvironmentExplorer.Views;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Windows.Input;
using System;
using System.Threading.Tasks;
using SMAStudiovNext.Modules.ConnectionManager.Windows;
using Gemini.Framework.Threading;

namespace SMAStudiovNext.Modules.EnvironmentExplorer.ViewModels
{
    [Export(typeof(EnvironmentExplorerViewModel))]
    public class EnvironmentExplorerViewModel : Tool, ICommandHandler<NewConnectionCommandDefinition>
    {
        private readonly ObservableCollection<ResourceContainer> _items;
        private readonly ICommand _publishCommand;
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
        
        /*/// <summary>
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
        }*/

        public override string DisplayName
        {
            get { return "Environment Explorer"; }
        }
        #endregion

        public EnvironmentExplorerViewModel()
        {
            _items = new ObservableCollection<ResourceContainer>();
            _publishCommand = new PublishCommand();
        }

        protected override void OnViewLoaded(object view)
        {
            _view = (IEnvironmentExplorerView)view;
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

        void ICommandHandler<NewConnectionCommandDefinition>.Update(Command command)
        {
            // Ignore
        }

        Task ICommandHandler<NewConnectionCommandDefinition>.Run(Command command)
        {
            var connManagerWindow = new ConnectionManagerWindow();
            connManagerWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            connManagerWindow.ShowDialog();

            return TaskUtility.Completed;
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

        public ICommand PublishCommand
        {
            get {
                return _publishCommand;
            }
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

        public ICommand NewModuleCommand
        {
            get { return AppContext.Resolve<ICommand>("NewModuleCommand"); }
        }

        public ICommand NewConnectionObjectCommand
        {
            get { return AppContext.Resolve<ICommand>("NewConnectionObjectCommand"); }
        }

        public ICommand NewConnectionCommand
        {
            get { return AppContext.Resolve<ICommand>("NewConnectionCommand"); }
        }

        public ICommand RefreshCommand
        {
            get { return AppContext.Resolve<ICommand>("RefreshCommand"); }
        }

        public ICommand DocumentationCommand
        {
            get { return AppContext.Resolve<ICommand>("DocumentationCommand"); }
        }
    }
}
