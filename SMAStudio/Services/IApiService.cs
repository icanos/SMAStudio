using SMAStudio.SMAWebService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Util
{
    public interface IApiService
    {
        OrchestratorApi Current { get; }
    }
}
