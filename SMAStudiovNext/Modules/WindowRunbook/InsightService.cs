using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using SMAStudiovNext.Modules.WindowRunbook.Editor;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Debugging;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Inspector;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Parser;

namespace SMAStudiovNext.Modules.WindowRunbook
{
    /// <summary>
    ///     Responsible for displaying Insight information when hovering a variable/cmdlet when in debug mode.
    /// </summary>
    public class InsightService : IDisposable
    {
        private readonly DebuggerService _debuggerService;
        private readonly RunbookEditor _editor;
        private readonly LanguageContext _languageContext;
        private Token _currentToken;

        private MouseEventArgs _lastKnownMousePosition;

        private ObjectInspectorWindow _objectInspector;

        public InsightService(RunbookEditor editor, LanguageContext languageContext, DebuggerService debuggerService)
        {
            _editor = editor;
            _languageContext = languageContext;
            _debuggerService = debuggerService;

            AttachEvents();
        }

        public void Dispose()
        {
            DetachEvents();
        }

        private void AttachEvents()
        {
            _editor.ToolTipRequest += OnToolTipRequest;
            _editor.MouseHover += OnMouseHover;
            _editor.MouseMove += OnMouseMove;
            _editor.LostFocus += OnLostFocus;
        }

        private void DetachEvents()
        {
            _editor.ToolTipRequest -= OnToolTipRequest;
            _editor.MouseHover -= OnMouseHover;
            _editor.MouseMove -= OnMouseMove;
            _editor.LostFocus -= OnLostFocus;
        }

        /// <summary>
        ///     Gets called when the window looses focus. If the inspector window is open, we want to close it at this point.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (_objectInspector == null)
                return;

            _objectInspector?.Close();
            _currentToken = null;
        }

        /// <summary>
        ///     Gets called when we move the mouse within the editor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            _lastKnownMousePosition = e;

            if (_objectInspector == null)
                return;

            if (_currentToken == null)
            {
                _objectInspector?.Close();
            }
        }

        private void OnMouseHover(object sender, MouseEventArgs e)
        {
            if (_objectInspector == null)
                return;

            var pos = _editor.GetPositionFromPoint(e.GetPosition(_editor));

            if (!pos.HasValue)
            {
                _objectInspector.Close();
                _currentToken = null;
                return;
            }

            if (pos.Value.Column < _currentToken.Extent.StartColumnNumber ||
                pos.Value.Column > _currentToken.Extent.EndColumnNumber)
            {
                _objectInspector.Close();
                _currentToken = null;
            }
        }

        private void OnToolTipRequest(object sender, ToolTipRequestEventArgs args)
        {
            if (!_debuggerService.IsActiveDebugging)
                return;

            var tokens =
                _languageContext.Tokens.Where(
                    x =>
                        x.Extent.StartLineNumber == args.LogicalPosition.Line &&
                        x.Extent.StartColumnNumber <= args.LogicalPosition.Column &&
                        x.Extent.EndColumnNumber >= args.LogicalPosition.Column)
                    .ToList();

            if (tokens.Count > 0)
            {
                var token = tokens.First();

                if (_objectInspector != null)
                {
                    // We already have an object inspector open, close that.
                    if (_currentToken != token)
                    {
                        _objectInspector.Close();
                        _currentToken = null;
                    }
                }

                _currentToken = token;

                switch (token.Kind)
                {
                    case TokenKind.Variable:
                        ShowVariableInsights(token, args);
                        break;
                    default:
                        args.ContentToShow = token.Kind + ": " + token.Text;
                        break;
                }
            }
            else
            {
                _objectInspector?.Close();
                _currentToken = null;
            }
        }

        private void ShowVariableInsights(Token token, ToolTipRequestEventArgs args)
        {
            var stackFrames = _debuggerService.GetStackFrames();
            var stackFrame = stackFrames?.FirstOrDefault();

            if (stackFrame == null)
                return;

            var variable =
                stackFrame?.LocalVariables.Children.FirstOrDefault(
                    x => x.Name.Equals(token.Text, StringComparison.InvariantCultureIgnoreCase));

            if (variable == null)
            {
                // Make sure to unset _currentToken since we can't find a variable named what we've selected.
                _currentToken = null;
                return;
            }

            // Create an inspectable object
            var inspectableObject = new Dictionary<string, object>
            {
                {variable.Name, variable.Value}
            };

            ShowInspectorWindow(inspectableObject);
        }

        private void ShowInspectorWindow(Dictionary<string, object> inspectableObject)
        {
            // Find the last known cursor position and place the caret there
            var visualColumn = 0;
            var isAtEndOfLine = false;
            var position = GetOffsetFromMousePosition(_lastKnownMousePosition, out visualColumn, out isAtEndOfLine);

            _editor.TextArea.Caret.Position = new TextViewPosition(_editor.TextArea.Document.GetLocation(position),
                visualColumn);
            _editor.TextArea.Caret.DesiredXPos = double.NaN;

            // Open the inspector
            _objectInspector = new ObjectInspectorWindow(_editor.TextArea)
            {
                SelectedObject = new InspectableDictionaryObject(inspectableObject),
                SizeToContent = SizeToContent.WidthAndHeight
            };

            _objectInspector.Closed += (sender, eventArgs) => _objectInspector = null;
            _objectInspector.Show();
        }

        private int GetOffsetFromMousePosition(MouseEventArgs e, out int visualColumn, out bool isAtEndOfLine)
        {
            return GetOffsetFromMousePosition(e.GetPosition(_editor.TextArea.TextView), out visualColumn,
                out isAtEndOfLine);
        }

        private int GetOffsetFromMousePosition(Point positionRelativeToTextView, out int visualColumn,
            out bool isAtEndOfLine)
        {
            visualColumn = 0;
            var textView = _editor.TextArea.TextView;
            var pos = positionRelativeToTextView;

            if (pos.Y < 0)
                pos.Y = 0;

            if (pos.Y > textView.ActualHeight)
                pos.Y = textView.ActualHeight;

            pos += textView.ScrollOffset;

            if (pos.Y >= textView.DocumentHeight)
                pos.Y = textView.DocumentHeight - 0.01;

            var line = textView.GetVisualLineFromVisualTop(pos.Y);
            if (line != null)
            {
                //visualColumn = line.GetVisualColumn(pos, _editor.TextArea.Selection.EnableVirtualSpace, out isAtEndOfLine);
                visualColumn = line.GetVisualColumn(pos, _editor.TextArea.Selection.EnableVirtualSpace);
                isAtEndOfLine = false;
                return line.GetRelativeOffset(visualColumn) + line.FirstDocumentLine.Offset;
            }
            isAtEndOfLine = false;
            return -1;
        }
    }
}