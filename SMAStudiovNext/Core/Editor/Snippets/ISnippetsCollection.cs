using System.Collections.Generic;

namespace SMAStudiovNext.Core.Editor.Snippets
{
    public interface ISnippetsCollection
    {
        IList<CodeSnippet> Snippets { get; set; }
    }
}
