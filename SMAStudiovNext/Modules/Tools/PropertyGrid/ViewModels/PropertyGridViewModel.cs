using System;
using System.ComponentModel.Composition;
using Gemini.Framework;
using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Modules.UndoRedo.Commands;

namespace SMAStudiovNext.Modules.PartPropertyGrid.ViewModels
{
    [Export(typeof(IPropertyGrid))]
    public sealed class PropertyGridViewModel : Tool, IPropertyGrid, ICommandRerouter
    {
        private readonly IShell _shell;

        public override PaneLocation PreferredLocation
        {
            get { return PaneLocation.Right; }
        }

        public override Uri IconSource
        {
            get { return new Uri("pack://application:,,,/SMAStudiovNext;component/Icons/Properties.png"); }
        }

        private object _selectedObject;
        public object SelectedObject
        {
            get { return _selectedObject; }
            set
            {
                _selectedObject = value;
                NotifyOfPropertyChange(() => SelectedObject);
            }
        }

        [ImportingConstructor]
        public PropertyGridViewModel(IShell shell)
        {
            _shell = shell;
        }

        public override string DisplayName
        {
            get { return "Properties"; }
        }

        object ICommandRerouter.GetHandler(CommandDefinitionBase commandDefinition)
        {
            if (commandDefinition is UndoCommandDefinition ||
                commandDefinition is RedoCommandDefinition)
            {
                return _shell.ActiveItem;
            }

            return null;
        }
    }
}
