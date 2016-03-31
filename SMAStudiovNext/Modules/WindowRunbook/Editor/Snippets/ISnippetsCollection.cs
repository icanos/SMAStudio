using System.Collections.Generic;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor.Snippets
{
    public interface ISnippetsCollection
    {
        IList<CodeSnippet> Snippets { get; set; }
    }
}
