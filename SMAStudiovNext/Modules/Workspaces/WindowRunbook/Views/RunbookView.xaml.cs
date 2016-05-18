using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SMAStudiovNext.Modules.WindowRunbook.Editor;

namespace SMAStudiovNext.Modules.WindowRunbook.Views
{
    /// <summary>
    /// Interaction logic for PowershellEditorView.xaml
    /// </summary>
    public partial class RunbookView : UserControl, IRunbookView
    {
        private readonly TextMarkerService _textMarkerService;

        public RunbookEditor TextEditor
        {
            get { return CodeEditor; }
        }

        public RunbookEditor PublishedTextEditor
        {
            get { return PublishedCodeEditor; }
        }

        public TextMarkerService TextMarkerService
        {
            get { return _textMarkerService; }
        }
        
        public RunbookView()
        {
            InitializeComponent();
            //Loaded += (sender, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

            _textMarkerService = new TextMarkerService(TextEditor);
            TextEditor.TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
            TextEditor.TextArea.TextView.LineTransformers.Add(_textMarkerService);

            TextEditor.MouseRightButtonDown += OnMouseRightButtonDown;
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is RunbookEditor))
                return;

            var position = ((RunbookEditor)sender).GetPositionFromPoint(e.GetPosition((IInputElement)sender));
            if (position.HasValue)
            {
                ((RunbookEditor)sender).TextArea.Caret.Position = position.Value;
            }
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender == m_scrollview_A)
            {
                m_scrollview_B.ScrollToVerticalOffset(m_scrollview_A.VerticalOffset);
                m_scrollview_B.ScrollToHorizontalOffset(m_scrollview_A.HorizontalOffset);
                return;
            }
            if (sender == m_scrollview_B)
            {
                m_scrollview_A.ScrollToVerticalOffset(m_scrollview_B.VerticalOffset);
                m_scrollview_A.ScrollToHorizontalOffset(m_scrollview_B.HorizontalOffset);
                return;
            }
        }

        /// <summary>
		/// Work around a limitation in the framework that does not allow to stretch ListBoxItemTemplates
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void StretchDataTemplate(object sender, RoutedEventArgs e)
        {
            // found this method at: http://silverlight.net/forums/p/18918/70469.aspx#70469
            var t = sender as FrameworkElement;
            if (t == null)
                return;
            var p = VisualTreeHelper.GetParent(t) as ContentPresenter;
            if (p == null)
                return;
            p.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
    }
}
