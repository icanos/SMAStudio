using SMAStudiovNext.Core;
using SMAStudiovNext.Utils;

namespace SMAStudiovNext.Modules.PartEnvironmentExplorer.Views
{
    public interface IEnvironmentExplorerView
    {
        void RefreshSource();

        IBackendContext GetCurrentContext();

        ResourceContainer SelectedObject { get; }
    }
}
