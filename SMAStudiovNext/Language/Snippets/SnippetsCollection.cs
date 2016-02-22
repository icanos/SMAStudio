using System.Collections.Generic;

namespace SMAStudiovNext.Language.Snippets
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
