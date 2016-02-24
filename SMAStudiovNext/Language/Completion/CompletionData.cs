using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.CodeCompletion;
using SMAStudiovNext.Language.Snippets;
using System.Windows.Media.Imaging;
using SMAStudiovNext.Icons;
using System.Collections.Generic;

namespace SMAStudiovNext.Language.Completion
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public abstract class CompletionDataBase : ICompletionData
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        public CompletionDataBase()
        {

        }

        public CompletionDataBase(string name)
        {
            Name = name;
        }

        public string DisplayText
        {
            get;
            set;
        }

        public virtual string Description
        {
            get; set;
        }

        public string Name
        {
            get; set;
        }
        
        #region ICompletionData
        public virtual ImageSource Image
        {
            get
            {
                return null;
            }
        }

        public string Text
        {
            get
            {
                return Name;
            }
        }

        public object Content
        {
            get
            {
                return DisplayText;
            }
        }

        object ICompletionData.Description
        {
            get
            {
                return null;
            }
        }

        public double Priority
        {
            get
            {
                return 0;
            }
        }

        #endregion

        public override string ToString()
        {
            return DisplayText;
        }

        public override bool Equals(object obj)
        {
            if (obj is CompletionDataBase)
                return ((CompletionDataBase)obj).Name.Equals(Name);

            return Name.Equals(obj);
        }

        public virtual void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            var text = textArea.Document.Text;
            var caretOffset = textArea.Caret.Offset;
            int startOffset = 0;

            string word = "";

            for (int i = caretOffset - 1; i >= 0; i--)
            {
                var ch = text[i];

                if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r' || ch == '(' || ch == ':')
                {
                    startOffset = i + 1;
                    break;
                }

                word = text[i] + word;
            }

            var segment = new TextSegment();
            segment.StartOffset = startOffset;
            segment.EndOffset = caretOffset;

            /*if (CodeCompletionContext != null && (this is KeywordCompletionData))
            {
                Console.WriteLine(this.ToString());
                CodeCompletionContext.CurrentKeyword = (ICompletionEntry)this;

                Task.Run(delegate ()
                {
                    CodeCompletionContext.GetParameters();
                });
            }*/

            textArea.Document.Replace(segment, Name);
        }
    }

    #region VariableCompletionData

    public class VariableCompletionData : CompletionDataBase, ICompletionEntry
    {
        public VariableCompletionData()
        {

        }

        public VariableCompletionData(string name, string typeName)
        {
            Name = name;
            Type = typeName;

            if (!String.IsNullOrEmpty(Type))
                DisplayText = Name + " : " + Type;
            else
                DisplayText = Name;
        }

        public override ImageSource Image
        {
            get
            {
                return new BitmapImage(new Uri(IconsDescription.Variable, UriKind.RelativeOrAbsolute));
            }
        }

        public string Type
        {
            get; set;
        }
    }
    #endregion

    #region SnippetCompletionData
    public class SnippetCompletionData : CompletionDataBase, ICompletionEntry
    {
        private readonly CodeSnippet _snippet;

        public SnippetCompletionData()
        {

        }

        public SnippetCompletionData(CodeSnippet snippet)
        {
            _snippet = snippet;
            Name = _snippet.Name;
            DisplayText = Name;
        }

        public override ImageSource Image
        {
            get
            {
                return new BitmapImage(new Uri(IconsDescription.Snippet, UriKind.RelativeOrAbsolute));
            }
        }

        public override void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            var text = textArea.Document.Text;
            var caretOffset = textArea.Caret.Offset;
            int startOffset = 0;

            string word = "";

            for (int i = caretOffset - 1; i >= 0; i--)
            {
                var ch = text[i];

                if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r' || ch == '(')
                {
                    startOffset = i + 1;
                    break;
                }

                word = text[i] + word;
            }

            var segment = new TextSegment();
            segment.StartOffset = startOffset;
            segment.EndOffset = caretOffset;

            textArea.Document.Replace(segment, "");

            _snippet.CreateAvalonEditSnippet(null).Insert(textArea);
        }
    }
    #endregion

    #region KeywordCompletionData
    public class KeywordCompletionData : CompletionDataBase, ICompletionEntry
    {
        private readonly string _icon;

        public KeywordCompletionData(string name)
            : base(name)
        {
            Parameters = new List<ICompletionEntry>();
            DisplayText = name;
        }

        /*public KeywordCompletionData(string name)
            : this(name)
        {
            //CodeCompletionContext = completionContext;
        }*/

        public KeywordCompletionData(string name, string icon)
            : this(name)
        {
            //CodeCompletionContext = completionContext;

            _icon = icon;
        }

        public KeywordCompletionData(string name, string icon, string description)
            : this(name)
        {
            Description = description;
            _icon = icon;
        }

        /*public KeywordCompletionData(string name, string module, ICodeCompletionContext completionContext)
            : this(name)
        {
            Module = module;
            CodeCompletionContext = completionContext;
        }*/

        /*public KeywordCompletionData(string name, string module, string icon)
            : this(name)
        {
            Module = module;
            _icon = icon;
        }

        public KeywordCompletionData(string name, string module, ICodeCompletionContext completionContext, string icon)
            : this(name)
        {
            Module = module;
            CodeCompletionContext = completionContext;

            _icon = icon;
        }*/

        public override ImageSource Image
        {
            get
            {
                return new BitmapImage(new Uri(_icon, UriKind.RelativeOrAbsolute));
            }
        }

        public string Module { get; set; }

        public IList<ICompletionEntry> Parameters { get; set; }
    }
    #endregion

    #region ParameterCompletionData
    public class ParameterCompletionData : CompletionDataBase, ICompletionEntry
    {
        public ParameterCompletionData()
        {

        }

        public ParameterCompletionData(string name)
        {
            Name = name;

            //if (!Name.StartsWith("-"))
            //    Name = "-" + name;
            if (Name.StartsWith("-"))
                Name = Name.Substring(1);

            if (!String.IsNullOrEmpty(Type))
                DisplayText = Name + " : " + Type;
            else
                DisplayText = Name;

            //if (!Name.StartsWith("-"))
            DisplayText = "-" + Name;
        }

        public ParameterCompletionData(string name, string typeName)
        {
            Name = name;

            Type = typeName;

            if (!Name.StartsWith("-"))
                Name = "-" + name;

            if (!String.IsNullOrEmpty(Type))
                DisplayText = Name + " : " + Type;
            else
                DisplayText = Name;

            if (!Name.StartsWith("-"))
                DisplayText = "-" + Name;
        }

        public ParameterCompletionData(string name, string typeName, bool includeDash = true)
        {
            Name = name;

            Type = typeName;

            if (!Name.StartsWith("-") && includeDash)
                Name = "-" + name;

            if (!String.IsNullOrEmpty(Type))
                DisplayText = Name + " : " + Type;
            else
                DisplayText = Name;

            if (!Name.StartsWith("-") && includeDash)
                DisplayText = "-" + Name;
        }

        public ParameterCompletionData(string name, string typeName, string description, bool includeDash = true)
        {
            Name = name;

            Type = typeName;

            if (!Name.StartsWith("-") && includeDash)
                Name = "-" + name;

            if (!String.IsNullOrEmpty(Type))
                DisplayText = Name + " : " + Type;
            else
                DisplayText = Name;

            if (!Name.StartsWith("-") && includeDash)
                DisplayText = "-" + Name;

            Description = description;
        }

        public string Type { get; set; }

        public bool IsArray { get; set; }

        public bool IsRequired { get; set; }

        public string Value { get; set; }

        public string RawName { get; set; }

        public string DescribingName
        {
            get
            {
                return Type + ": " + Name + (IsRequired ? " (required)" : "");
            }
        }
    }
    #endregion
}
