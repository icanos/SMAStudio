using System;
using System.Management.Automation.Language;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using SMAStudiovNext.Core;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Parser;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Renderers;
using SMAStudiovNext.Modules.WindowRunbook.Resources;
using SMAStudiovNext.Modules.WindowRunbook.SyntaxHighlightning;
using SMAStudiovNext.Themes;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor
{
    public delegate void ToolTipRequestEventHandler(object sender, ToolTipRequestEventArgs args);

    /// <summary>
    /// This is our custom implementation of the AvalonEdit editor
    /// to be able to add support for code completion and reference counting etc.
    /// 
    /// This is very much based upon Gemini.Modules.CodeEditor
    /// </summary>
    public class RunbookEditor : TextEditor
    {
        private FoldingManager _foldingManager;
        private readonly PowershellFoldingStrategy _foldingStrategy;
        private readonly LanguageContext _languageContext;
        private ThemedHighlightingColorizer _colorizer;
        private readonly BracketHighlightRenderer _bracketRenderer;
        private ToolTip _toolTip;

        public RunbookEditor()
        {
            _languageContext = new LanguageContext();

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

            MouseHover += OnMouseHover;
            MouseHoverStopped += OnMouseHoverStopped;
            TextArea.Caret.PositionChanged += HighlightBrackets;

            _foldingStrategy = new PowershellFoldingStrategy();

            var foldingUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            foldingUpdateTimer.Tick += delegate { UpdateFoldings(); };
            foldingUpdateTimer.Start();

            _bracketRenderer = new BracketHighlightRenderer(this.TextArea.TextView, _languageContext);
            TextArea.TextView.BackgroundRenderers.Add(_bracketRenderer);

            InitializeColorizer();
        }

        private async void HighlightBrackets(object sender, EventArgs eventArgs)
        {
            if (TextArea.Caret.Offset < 1)
            {
                _bracketRenderer.SetHighlight(null);
                return;
            }

            var charBack = Text[TextArea.Caret.Offset - 1];
            var charFront = Text.Length > TextArea.Caret.Offset ? Text[TextArea.Caret.Offset] : '\0';
            var character = '\0';
            var isInFrontOf = false;

            if (KeystrokeService.IsBracketOrParen(charBack))
            {
                character = charBack;
            }
            else if (KeystrokeService.IsBracketOrParen(charFront))
            {
                character = charFront;
                isInFrontOf = true;
            }

            if (character != '\0')
            {
                var openToken = default(TokenKind);//character == '{' ? TokenKind.LCurly : TokenKind.LParen;
                var closeToken = default(TokenKind);// character == '}' ? TokenKind.RCurly : TokenKind.RParen;
                var startWithClose = false;

                if (character == '{' || character == '}')
                {
                    openToken = TokenKind.LCurly;
                    closeToken = TokenKind.RCurly;
                }
                else
                {
                    openToken = TokenKind.LParen;
                    closeToken = TokenKind.RParen;
                }

                if (character == '}' || character == ')')
                    startWithClose = true;

                var offset = TextArea.Caret.Offset;
                if (!isInFrontOf)
                    offset -= 1;

                var result = await _languageContext.FindBracketMatch(offset, openToken, closeToken, startWithClose);

                if (result == null)
                {
                    _bracketRenderer.SetHighlight(null);
                    return;
                }

                _bracketRenderer.SetHighlight(result);
            }
            else
            {
                _bracketRenderer.SetHighlight(null);
            }
        }

        private void OnMouseHover(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var position = TextArea.TextView.GetPositionFloor(e.GetPosition(TextArea.TextView) + TextArea.TextView.ScrollOffset);
            var args = new ToolTipRequestEventArgs { InDocument = position.HasValue };

            if (!position.HasValue || position.Value.Location.IsEmpty)
            {
                return;
            }

            args.LogicalPosition = position.Value.Location;

            RaiseEvent(args);

            if (args.ContentToShow == null) return;

            if (_toolTip == null)
            {
                _toolTip = new ToolTip { MaxWidth = 300 };
                _toolTip.Closed += OnToolTipClosed;

                ToolTipService.SetInitialShowDelay(_toolTip, 0);
            }

            _toolTip.PlacementTarget = this;

            var stringContent = args.ContentToShow as string;

            if (stringContent != null)
            {
                _toolTip.Content = new TextBlock
                {
                    Text = stringContent,
                    TextWrapping = TextWrapping.Wrap
                };
            }
            else
            {
                _toolTip.Content = args.ContentToShow;
            }

            e.Handled = true;
            _toolTip.IsOpen = true;
        }

        private void OnMouseHoverStopped(object sender, MouseEventArgs e)
        {
            if (_toolTip != null)
            {
                _toolTip.IsOpen = false;
                e.Handled = true;
            }
        }

        private void OnToolTipClosed(object sender, EventArgs e)
        {
            _toolTip = null;
        }

        public static readonly RoutedEvent ToolTipRequestEvent = EventManager.RegisterRoutedEvent("ToolTipRequest",
            RoutingStrategy.Bubble, typeof(ToolTipRequestEventHandler), typeof(RunbookEditor));

        public event ToolTipRequestEventHandler ToolTipRequest
        {
            add { AddHandler(ToolTipRequestEvent, value); }
            remove { RemoveHandler(ToolTipRequestEvent, value); }
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
            Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom(themeManager.CurrentTheme.Foreground));

            themeManager.UpdateCurrentTheme += delegate ()
            {
                FontFamily = new FontFamily(themeManager.CurrentTheme.Font);
                FontSize = themeManager.CurrentTheme.FontSize;
                Background = (SolidColorBrush)(new BrushConverter().ConvertFrom(themeManager.CurrentTheme.Background));
                Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom(themeManager.CurrentTheme.Foreground));
            };
            
            _foldingManager = FoldingManager.Install(TextArea);
            UpdateFoldings();
        }

        private void InitializeColorizer()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SMAStudiovNext.Modules.WindowRunbook.SyntaxHighlightning.Powershell.xshd";

            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                var reader = new XmlTextReader(stream);

                SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            
                _colorizer = new ThemedHighlightingColorizer(SyntaxHighlighting);
                TextArea.TextView.LineTransformers.Add(_colorizer);

                reader.Close();
                stream.Close();
            }
        }
        
        public LanguageContext LanguageContext
        {
            get { return _languageContext; }
        }

        private void UpdateFoldings()
        {
            _foldingStrategy.UpdateFoldings(_foldingManager, Document);
        }
    }
}
