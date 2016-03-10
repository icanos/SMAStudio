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
using SMAStudiovNext.Core;
using SMAStudiovNext.Themes;
using SMAStudiovNext.Modules.Runbook.Editor.Parser;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SMAStudiovNext.Modules.Runbook.Editor
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
        private PowershellFoldingStrategy _foldingStrategy;
        private LanguageContext _languageContext;
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

            _foldingStrategy = new PowershellFoldingStrategy();

            var foldingUpdateTimer = new DispatcherTimer();
            foldingUpdateTimer.Interval = TimeSpan.FromSeconds(4);
            foldingUpdateTimer.Tick += delegate { UpdateFoldings(); };
            foldingUpdateTimer.Start();

            InitializeColorizer();
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
            //_colorizer = new SyntaxHighlightning.HighlightingColorizer(_languageContext);
            //TextArea.TextView.LineTransformers.Add(_colorizer);

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SMAStudiovNext.Modules.WindowRunbook.SyntaxHighlightning.Powershell.xshd";

            var stream = assembly.GetManifestResourceStream(resourceName);
            var reader = new XmlTextReader(stream);

            SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);

            reader.Close();
            stream.Close();
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
