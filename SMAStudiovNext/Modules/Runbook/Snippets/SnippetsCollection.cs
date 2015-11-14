using System.Collections.Generic;

namespace SMAStudiovNext.Modules.Runbook.Snippets
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
