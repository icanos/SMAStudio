using SMAStudiovNext.Core;
using SMAStudiovNext.Utils;
using System.Windows.Controls;

namespace SMAStudiovNext.Modules.PartEnvironmentExplorer.Views
{
    public interface IEnvironmentExplorerView
    {
        void RefreshSource();

        IBackendContext GetCurrentContext();

        ResourceContainer SelectedObject { get; }

        MenuItem CopyButton { get; }
    }
}
