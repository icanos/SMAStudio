using System.Collections.Generic;

namespace SMAStudiovNext.Core.Editor.Snippets
{
    public class SnippetsCollection : ISnippetsCollection
    {
        public SnippetsCollection()
        {
            Snippets = new List<CodeSnippet>();
        }

        public IList<CodeSnippet> Snippets { get; set; }
    }
}
