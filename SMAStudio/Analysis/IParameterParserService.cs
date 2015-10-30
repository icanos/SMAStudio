using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Analysis
{
    public interface IParameterParserService
    {
        void Start();

        IList<UIInputParameter> GetParameters(string runbookName);

        void NotifyChanges();
    }
}
