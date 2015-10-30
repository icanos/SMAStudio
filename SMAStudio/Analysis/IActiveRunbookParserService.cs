using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Editor.Parsing
{
    public interface IActiveRunbookParserService
    {
        void Start();

        void ParseCommandTokens(IDocumentViewModel document);
    }
}
