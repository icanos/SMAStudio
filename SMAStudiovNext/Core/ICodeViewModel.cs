using ICSharpCode.AvalonEdit.CodeCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core
{
    public interface ICodeViewModel
    {
        IList<ICompletionData> GetParameters(string completionWord);

        Guid Id { get; }

        string Content { get; }

        string Name { get; }

        DateTime LastKeyStroke { get; set; }
    }
}
