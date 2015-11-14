using SMAStudiovNext.Agents;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading.Tasks;
using SMAStudiovNext.Core;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System.Windows.Media;
using SMAStudiovNext.Modules.Runbook.Snippets;
using System.Windows.Media.Imaging;
using SMAStudiovNext.Icons;

namespace SMAStudiovNext.Modules.Runbook.CodeCompletion
{
    /// <summary>
    /// TODO: Implement a powershell module resolver that can cache requests in powershell to speed up!
    /// </summary>
    public class CodeCompletionContext : ICodeCompletionContext, ILocalCodeCompletionContext
    {
        #region Language Constructs
        private readonly IList<string> _keywords = new List<string>
        {
            "Begin", "Break", "Catch", "Continue", "Data", "Do", "DynamicParam", "Else", "ElseIf", "End",
            "Exit", "Filter", "Finally", "For", "ForEach", "From", "Function", "If", "In", "InlineScript",
            "Hidden", "Parallel", "Param", "Process", "Return", "Sequence", "Switch", "Throw", "Trap", "Try",
            "Until", "While", "Workflow"
        };

        private readonly IList<string> _defaultParameters = new List<string>
        {
            "Debug", "ErrorAction", "ErrorVariable", "InformationAction", "InformationVariable",
            "OutVariable", "OutBuffer", "PiplineVariable", "Verbose", "WarningAction",
            "WarningVariable", "WhatIf", "Confirm"
        };

        private readonly IList<string> _smaCmdlets = new List<string>
        {
            "Get-AutomationVariable", "Get-AutomationPSCredential", "Get-AutomationCertificate", "Set-AutomationVariable", "Get-AutomationConnection"
        };
        #endregion

        private readonly GlobalCodeCompletionContext _globalCompletionContext;

        public CodeCompletionContext()
        {
            _globalCompletionContext = (GlobalCodeCompletionContext)AppContext.Resolve<IAgent>("GlobalCodeCompletionContext");

            Variables = new List<ICompletionEntry>();
            Keywords = new List<ICompletionEntry>();
            AllModules = new List<string>();
            UsedModules = new Dictionary<string, IList<string>>();
        }

        #region Properties
        public IList<ICompletionEntry> Variables { get; set; }

        public IList<ICompletionEntry> Keywords { get; set; }

        public IList<ICompletionEntry> GlobalKeywords { get { return _globalCompletionContext.Keywords; } }

        public IList<ICompletionEntry> GlobalRunbooks { get { return _globalCompletionContext.Runbooks; } }

        public IList<string> AllModules { get; set; }

        public IDictionary<string, IList<string>> UsedModules { get; set; }

        public ICompletionEntry CurrentKeyword { get; set; }
        #endregion

        public void AddModule(string runbookName, string moduleName)
        {
            if (UsedModules.ContainsKey(runbookName) && UsedModules[runbookName].Contains(moduleName))
                return;

            var alreadyLoaded = UsedModules.Values.FirstOrDefault(v => v.Equals(moduleName));
            if (alreadyLoaded != null)
            {
                if (UsedModules.ContainsKey(runbookName))
                    UsedModules[runbookName].Add(moduleName);
                else
                {
                    UsedModules.Add(runbookName, new List<string>());
                    UsedModules[runbookName].Add(moduleName);
                }

                return;
            }

            // Enumerate the used module to find out which commands exists inside the module
            AsyncExecution.Run(System.Threading.ThreadPriority.BelowNormal, delegate ()
            {
                try {
                    using (var context = PowerShell.Create())
                    {
                        context.AddScript("Import-Module " + moduleName + "; Get-Command -Module " + moduleName);
                        var commands = context.Invoke();

                        var parallelOptions = new ParallelOptions();
                        parallelOptions.MaxDegreeOfParallelism = 10;

                        Parallel.ForEach(commands, parallelOptions, (command) =>
                        {
                            Keywords.Add(new KeywordCompletionData(command.ToString(), moduleName, this, IconsDescription.Cmdlet));
                        });
                    }
                }
                catch (ParseException)
                {
                    // Ignore
                }
            });


            if (UsedModules.ContainsKey(runbookName))
                UsedModules[runbookName].Add(moduleName);
            else
            {
                UsedModules.Add(runbookName, new List<string>());
                UsedModules[runbookName].Add(moduleName);
            }
        }

        #region IAgent Implementation
        public void Start()
        {
            // Add language constructs
            foreach (var keyword in _keywords)
                Keywords.Add(new KeywordCompletionData(keyword, this, IconsDescription.LanguageConstruct));

            // SMA cmdlets
            foreach (var cmdlet in _smaCmdlets)
                Keywords.Add(new KeywordCompletionData(cmdlet, this, IconsDescription.Cmdlet));
        }

        public void Stop()
        {

        }
        #endregion

        public IList<ICompletionEntry> GetParameters()
        {
            if (CurrentKeyword == null && _globalCompletionContext.CurrentKeyword == null)
                return new List<ICompletionEntry>();

            if (CurrentKeyword != null)
                _globalCompletionContext.CurrentKeyword = CurrentKeyword;

            return _globalCompletionContext.GetParameters();
        }
    }

    #region CompletionDataBase
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public abstract class CompletionDataBase : ICompletionData
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        private ICodeCompletionContext _completionContext;

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

        public ICodeCompletionContext CodeCompletionContext
        {
            private get { return _completionContext; }
            set { _completionContext = value; }
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

            if (CodeCompletionContext != null && (this is KeywordCompletionData))
            {
                Console.WriteLine(this.ToString());
                CodeCompletionContext.CurrentKeyword = (ICompletionEntry)this;

                Task.Run(delegate ()
                {
                    CodeCompletionContext.GetParameters();
                });
            }

            textArea.Document.Replace(segment, Name);
        }
    }
    #endregion

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

        public KeywordCompletionData(string name, ICodeCompletionContext completionContext)
            : this(name)
        {
            CodeCompletionContext = completionContext;
        }

        public KeywordCompletionData(string name, ICodeCompletionContext completionContext, string icon)
            : this(name)
        {
            CodeCompletionContext = completionContext;

            _icon = icon;
        }

        public KeywordCompletionData(string name, string module)
            : this(name)
        {
            Module = module;
        }

        public KeywordCompletionData(string name, string module, ICodeCompletionContext completionContext)
            : this(name)
        {
            Module = module;
            CodeCompletionContext = completionContext;
        }

        public KeywordCompletionData(string name, string module, string icon)
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
        }

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
