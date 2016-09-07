using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core
{
    public interface IBackendContextManager
    {
        void Initialize();

        BackendContext Load(BackendConnection connection);

        void Refresh();
    }
}
