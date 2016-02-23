using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using SMAStudiovNext.Modules.Runbook.Resources;
using System;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using ICSharpCode.AvalonEdit.Rendering;
using SMAStudio.Language;
using SMAStudiovNext.Core;
using SMAStudiovNext.Themes;

namespace SMAStudiovNext.Modules.Runbook.Controls
{
    /// <summary>
    /// This is our custom implementation of the AvalonEdit editor
    /// to be able to add support for code completion and reference counting etc.
    /// 
    /// This is very much based upon Gemini.Modules.CodeEditor
    /// </summary>
    public class RunbookEditor : TextEditor
    {
        private FoldingManager _foldingManager;
        private PowershellFoldingStrategy _foldingStrategy;

        private SyntaxHighlightning.HighlightingColorizer _highlightingColorizer = null;

        public RunbookEditor()
        {
            FontFamily = new FontFamily("Consolas");
            FontSize = 12;
            ShowLineNumbers = true;
            Options = new TextEditorOptions
            {
                ConvertTabsToSpaces = true,
                HighlightCurrentLine = true,
                IndentationSize = 4,
                AllowScrollBelowDocument = true
            };

            _foldingStrategy = new PowershellFoldingStrategy();

            var foldingUpdateTimer = new DispatcherTimer();
            foldingUpdateTimer.Interval = TimeSpan.FromSeconds(4);
            foldingUpdateTimer.Tick += delegate { UpdateFoldings(); };
            foldingUpdateTimer.Start();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public override void BeginInit()
        {
            base.BeginInit();

            // Configure themeing
            var themeManager = AppContext.Resolve<IThemeManager>();
            FontFamily = new FontFamily(themeManager.CurrentTheme.Font);
            FontSize = themeManager.CurrentTheme.FontSize;
            Background = (SolidColorBrush)(new BrushConverter().ConvertFrom(themeManager.CurrentTheme.Background));

            _foldingManager = FoldingManager.Install(TextArea);
            UpdateFoldings();
        }

        public void InitializeColorizer(LanguageContext languageContext)
        {
            _highlightingColorizer = new SyntaxHighlightning.HighlightingColorizer(languageContext);

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SMAStudiovNext.Modules.Runbook.SyntaxHighlightning.Powershell.xshd";

            var stream = assembly.GetManifestResourceStream(resourceName);
            var reader = new XmlTextReader(stream);

            SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);

            reader.Close();
            stream.Close();
        }

        protected override IVisualLineTransformer CreateColorizer(IHighlightingDefinition highlightingDefinition)
        {
            return _highlightingColorizer;
        }
        
        private void UpdateFoldings()
        {
            _foldingStrategy.UpdateFoldings(_foldingManager, Document);
        }
    }
}
