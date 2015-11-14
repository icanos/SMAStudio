using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core
{
    public interface IStatusManager
    {
        void SetText(string message);

        void SetTimeoutText(string message, int timeoutInSeconds);
    }
}
