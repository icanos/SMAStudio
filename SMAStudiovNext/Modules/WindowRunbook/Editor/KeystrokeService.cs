using System.Linq;
using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Caliburn.Micro;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;
using SMAStudiovNext.Core;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Completion;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Parser;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor
{
    public class KeystrokeService
    {
        private readonly ICompletionProvider _completionProvider;
        private readonly LanguageContext _languageContext;
        private readonly TextArea _textArea;

        private CompletionWindow _completionWindow = null;
        private long _triggerTag;
        private bool _openedByControlSpace = false;

        public KeystrokeService(TextArea textArea, ICompletionProvider completionProvider, LanguageContext languageContext)
        {
            _completionProvider = completionProvider;
            _completionProvider.OnCompletionCompleted += OnCompletionResultRetrieved;
            _languageContext = languageContext;

            _textArea = textArea;
            _textArea.KeyUp += OnKeyReleased;
            _textArea.TextEntered += OnTextEntered;
        }

        #region Event Handlers
        private void OnTextEntered(object sender, TextCompositionEventArgs e)
        {
            var ch = e.Text[0];
            
            if ((IsCodeCompletionTrigger(ch) || char.IsLetter(ch)) && _completionWindow == null)
            {
                TriggerCompletion();
            }
        }

        private void OnKeyReleased(object sender, KeyEventArgs e)
        {
            // If we press the backspace key when completionWindow is open, we
            // close the window.
            if ((e.Key == Key.Back || e.Key == Key.Space) && _completionWindow != null)
            {
                _completionWindow.Close();
                _completionWindow = null;

                return;
            }

            // If enter or tab is pressed when the completionWindow is open,
            // request insertion of the selected word.
            if ((e.Key == Key.Enter || e.Key == Key.Tab) && _completionWindow != null)
            {
                _completionWindow.CompletionList.RequestInsertion(e);

                return;
            }
        }

        private void OnCompletionResultRetrieved(object sender, CompletionEventArgs e)
        {
            Execute.OnUIThread(() =>
            {
                if (_completionWindow == null && e.CompletionMatches.Any())
                {
                    _completionWindow = new CompletionWindow(_textArea)
                    {
                        CloseWhenCaretAtBeginning = _openedByControlSpace,
                        CloseAutomatically = true,
                        Width = 300
                    };
                    
                    var data = _completionWindow.CompletionList.CompletionData;
                    foreach (var completion in e.CompletionMatches)
                    {
                        data.Add(completion);
                    }

                    _completionWindow.Show();

                    _completionWindow.Closed += (o, args) =>
                    {
                        _completionWindow = null;
                    };
                }
            });
        }
        #endregion

        public void TriggerCompletion()
        {
            var caretPosition = -1;
            var lineTextUpToCaret = string.Empty;
            var script = string.Empty;

            Execute.OnUIThread(() =>
            {
                caretPosition = _textArea.Caret.Offset;

                // Get the text from the line which the caret is placed
                var line = _textArea.Document.GetLineByOffset(caretPosition);
                lineTextUpToCaret = _textArea.Document.GetText(line);
                script = _textArea.Document.Text;
            });

            if (CompletionUtils.IsInCommentArea(caretPosition, _textArea))
            {
                Logger.DebugFormat("Is in comment area, skip.");
                return;
            }

            var completionWord = GetWordNextToCaret(lineTextUpToCaret, lineTextUpToCaret.Length - 1);

            var token = default(Token);
            if ((token = IsPreviousTokenCommand(caretPosition)) != null)
            {
                // ReSharper disable once AssignmentInConditionalExpression
                if (_completionProvider.IsRunbook(token))
                {
                    // We need to retrieve completion for runbook parameters
                    Task.Factory.StartNew(() =>
                    {
                        Interlocked.Increment(ref _triggerTag);

                        _completionProvider.GetParameterCompletionData(token, completionWord, _triggerTag);
                    });

                    return;
                }
            }

            Task.Factory.StartNew(() =>
            {
                Interlocked.Increment(ref _triggerTag);

                _completionProvider.GetCompletionData(completionWord, script, lineTextUpToCaret, null, caretPosition,
                    null, _triggerTag);
            });
        }

        private string GetWordNextToCaret(string lineText, int caretLineOffset)
        {
            var word = string.Empty;

            for (var i = caretLineOffset; i >= 0; i--)
            {
                if (char.IsLetterOrDigit(lineText[i]) || lineText[i] == '_' || lineText[i] == '$' || lineText[i] == '-')
                {
                    word = lineText[i] + word;
                    continue;
                }

                break;
            }

            return word;
        }

        public static bool IsCodeCompletionTrigger(char ch)
        {
            return ch == '-' || ch == '$' || ch == ':';
        }

        public static bool IsBracketOrParen(char ch)
        {
            return ch == '(' || ch == ')' || ch == '{' || ch == '}';
        }

        private Token IsPreviousTokenCommand(int position)
        {
            var line = _textArea.Document.GetLineByOffset(position);
            var lineText = _textArea.Document.GetText(line);

            //Token[] tokens;
            //ParseError[] errors;
            //System.Management.Automation.Language.Parser.ParseInput(lineText, out tokens, out errors);

            if (_languageContext.Tokens.Length >= 2)
            {
                var filteredTokens = _languageContext.Tokens.Where(t => t.Extent.StartLineNumber == line.LineNumber && t.Extent.EndOffset <= position).ToList();

                if (filteredTokens.Count < 1)
                    return null;

                var token = filteredTokens[filteredTokens.Count - 1];

                if (token.TokenFlags == TokenFlags.CommandName)
                    return token;
            }

            return null;
        }
    }
}
