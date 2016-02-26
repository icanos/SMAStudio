using SMAStudiovNext.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.EnvironmentExplorer.Views
{
    public interface IEnvironmentExplorerView
    {
        void RefreshSource();

        IBackendContext GetCurrentContext();

        ResourceContainer SelectedObject { get; }
    }
}
