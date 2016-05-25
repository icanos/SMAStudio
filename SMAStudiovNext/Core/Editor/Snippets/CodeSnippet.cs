using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Snippets;
using SMAStudiovNext.Models;

namespace SMAStudiovNext.Core.Editor.Snippets
{
    /// <summary>
    /// Code is based on the SharpDevelop implementation of CodeSnippet.cs but has been customized to work with
    /// SMA Studio and runbooks.
    /// 
    /// Based on code from commit and file:
    /// https://raw.githubusercontent.com/icsharpcode/SharpDevelop/e454d66947b930d4bec9647f143e1e54ac568f63/src/AddIns/DisplayBindings/AvalonEdit.AddIn/Src/Snippets/CodeSnippet.cs
    /// 
    /// A code snippet.
    /// </summary>
    public class CodeSnippet : INotifyPropertyChanged
    {
        string name = string.Empty, description = string.Empty, text = string.Empty, keyword = string.Empty;

        public CodeSnippet()
        {
        }

        public CodeSnippet(CodeSnippet copy)
        {
            this.name = copy.name;
            this.description = copy.description;
            this.text = copy.text;
            this.keyword = copy.keyword;
        }

        #region Properties
        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value ?? string.Empty;
                    OnPropertyChanged("Name");
                }
            }
        }

        public string Text
        {
            get { return text; }
            set
            {
                if (text != value)
                {
                    text = value ?? string.Empty;
                    OnPropertyChanged("Text");
                }
            }
        }

        public string Description
        {
            get { return description; }
            set
            {
                if (description != value)
                {
                    description = value ?? string.Empty;
                    OnPropertyChanged("Description");
                }
            }
        }

        public bool HasSelection
        {
            get
            {
                return pattern.Matches(this.Text)
                    .OfType<Match>()
                    .Any(item => item.Value == "${Selection}");
            }
        }

        public string Keyword
        {
            get { return keyword; }
            set
            {
                if (keyword != value)
                {
                    keyword = value ?? string.Empty;
                    OnPropertyChanged("Keyword");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public Snippet CreateAvalonEditSnippet(RunbookModelProxy runbook)
        {
            return CreateAvalonEditSnippet(runbook, this.Text);
        }

        readonly static Regex pattern = new Regex(@"\$\{([^\}]*)\}", RegexOptions.CultureInvariant);

        public static Snippet CreateAvalonEditSnippet(RunbookModelProxy runbook, string snippetText)
        {
            if (snippetText == null)
                throw new ArgumentNullException("text");

            var replaceableElements = new Dictionary<string, SnippetReplaceableTextElement>(StringComparer.OrdinalIgnoreCase);

            foreach (Match m in pattern.Matches(snippetText))
            {
                string val = m.Groups[1].Value;
                int equalsSign = val.IndexOf('=');
                if (equalsSign > 0)
                {
                    string name = val.Substring(0, equalsSign);
                    replaceableElements[name] = new SnippetReplaceableTextElement();
                }
            }

            Snippet snippet = new Snippet();
            int pos = 0;

            foreach (Match m in pattern.Matches(snippetText))
            {
                if (pos < m.Index)
                {
                    snippet.Elements.Add(new SnippetTextElement { Text = snippetText.Substring(pos, m.Index - pos) });
                    pos = m.Index;
                }

                snippet.Elements.Add(CreateElementForValue(runbook, replaceableElements, m.Groups[1].Value, m.Index, snippetText));
                pos = m.Index + m.Length;
            }

            if (pos < snippetText.Length)
            {
                snippet.Elements.Add(new SnippetTextElement { Text = snippetText.Substring(pos) });
            }

            if (!snippet.Elements.Any(e => e is SnippetCaretElement))
            {
                var obj = snippet.Elements.FirstOrDefault(e2 => e2 is SnippetSelectionElement);
                int index = snippet.Elements.IndexOf(obj);

                if (index > -1)
                    snippet.Elements.Insert(index + 1, new SnippetCaretElement());
            }

            return snippet;
        }

        readonly static Regex functionPattern = new Regex(@"^([a-zA-Z]+)\(([^\)]*)\)$", RegexOptions.CultureInvariant);

        static SnippetElement CreateElementForValue(RunbookModelProxy runbook, Dictionary<string, SnippetReplaceableTextElement> replaceableElements, string val, int offset, string snippetText)
        {
            SnippetReplaceableTextElement srte;
            int equalsSign = val.IndexOf('=');

            if (equalsSign > 0)
            {
                string name = val.Substring(0, equalsSign);
                if (replaceableElements.TryGetValue(name, out srte))
                {
                    if (srte.Text == null)
                        srte.Text = val.Substring(equalsSign + 1);
                    return srte;
                }
            }
            
            if (replaceableElements.TryGetValue(val, out srte))
                return new SnippetBoundElement { TargetElement = srte };
            
            string result = GetValue(runbook, val);

            if (result != null)
                return new SnippetTextElement { Text = result };
            else
                return new SnippetReplaceableTextElement { Text = val }; // ${unknown} -> replaceable element
        }

        static string GetValue(RunbookModelProxy runbook, string propertyName)
        {
            if (runbook == null)
                return null;

            var property = runbook.GetType().GetProperty(propertyName);
            if (property != null)
            {
                return property.GetValue(runbook).ToString();
            }
            
            return null;
        }
    }
}
