using System.Linq;
using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Caliburn.Micro;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using SMAStudiovNext.Core;
using System;
using SMAStudiovNext.Modules.WindowRunbook.ViewModels;
using System.Windows.Threading;
using SMAStudiovNext.Core.Editor.Completion;
using SMAStudiovNext.Core.Editor.Parser;
using SMAStudiovNext.Core.Editor.Debugging;

namespace SMAStudiovNext.Core.Editor
{
    public class KeystrokeService
    {
        private readonly ICompletionProvider _completionProvider;
        private readonly LanguageContext _languageContext;
        private readonly TextArea _textArea;
        private readonly DebuggerService _debuggerService;
        private readonly BookmarkManager _bookmarkManager;
        private readonly ICodeViewModel _codeViewModel;

        private CompletionWindow _completionWindow = null;
        private long _triggerTag;
        private bool _openedByControlSpace = false;

        public KeystrokeService(ICodeViewModel codeViewModel, TextArea textArea, ICompletionProvider completionProvider, LanguageContext languageContext, DebuggerService debuggerService, BookmarkManager bookmarkManager)
        {
            _completionProvider = completionProvider;
            _completionProvider.OnCompletionCompleted += OnCompletionResultRetrieved;
            _languageContext = languageContext;
            _debuggerService = debuggerService;
            _bookmarkManager = bookmarkManager;
            _codeViewModel = codeViewModel;

            _textArea = textArea;
            _textArea.KeyUp += OnKeyReleased;
            _textArea.TextEntered += OnTextEntered;
        }

        #region Event Handlers
        private void OnTextEntered(object sender, TextCompositionEventArgs e)
        {
            var ch = e.Text[0];

            // Set last key stroke
            _codeViewModel.LastKeyStroke = DateTime.Now;

            // Notify our language context that the document is dirty and needs a reparsing
            _languageContext.IsDirty = true;

            // Update any parse errors to account for the new text inserted
            var caretOffset = _textArea.Caret.Offset;
            var lineText = _textArea.Document.GetText(_textArea.Document.GetLineByOffset(caretOffset));
            Task.Run(() =>
            {
                _bookmarkManager.RecalculateOffsets(_textArea, BookmarkType.ParseError, caretOffset, e.Text.Length);
                _bookmarkManager.RecalculateOffsets(_textArea, BookmarkType.AnalyzerInfo, caretOffset, e.Text.Length);
                _bookmarkManager.RecalculateOffsets(_textArea, BookmarkType.AnalyzerWarning, caretOffset, e.Text.Length);
            });

            //if ((IsCodeCompletionTrigger(ch) || char.IsLetter(ch)) && _completionWindow == null)
            if (_completionWindow == null && (IsCodeCompletionTrigger(ch) || string.IsNullOrEmpty(lineText.Trim())))// || IsCompletionPosition(caretOffset))
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

            // Debugging
            if (_debuggerService.IsActiveDebugging)
            {
                switch (e.Key)
                {
                    case Key.F10:
                        _debuggerService.StepOver();
                        break;
                    case Key.F11:
                        _debuggerService.StepInto();
                        break;
                }
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

            if (CompletionUtils.IsInCommentArea(caretPosition, _textArea) || CompletionUtils.IsInStringArea(caretPosition, _textArea))
            {
                Logger.DebugFormat("Is in comment area, skip.");
                return;
            }

            var completionWord = GetWordNextToCaret(lineTextUpToCaret, lineTextUpToCaret.Length - 1);

            /*var token = default(Token);
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
            }*/

            //var runbookToken = IsPreviousTokenCommand(caretPosition);
            var runbookToken = GetCommandFromCaretPosition(caretPosition);

            if (runbookToken != null && !_completionProvider.IsRunbook(runbookToken))
                runbookToken = null;

            Task.Factory.StartNew(() =>
            {
                Interlocked.Increment(ref _triggerTag);

                _completionProvider.GetCompletionData(completionWord, script, lineTextUpToCaret, null, runbookToken, caretPosition,
                    null, _triggerTag);
            });
        }

        /// <summary>
        /// Get the command name based on the caret offset
        /// </summary>
        /// <param name="caretPosition"></param>
        /// <returns>Token (of a runbook) or null if none found</returns>
        private Token GetCommandFromCaretPosition(int caretPosition)
        {
            var line = _textArea.Document.GetLineByOffset(caretPosition);
            var tokens = _languageContext.Tokens.Where(t => 
                                                    t.Extent.StartLineNumber == line.LineNumber && 
                                                    t.Extent.EndOffset <= caretPosition && 
                                                    t.TokenFlags == TokenFlags.CommandName);

            if (tokens.Count() == 0)
                return null;

            return tokens.Last();
        }

        private string GetWordNextToCaret(string lineText, int caretLineOffset)
        {
            var word = string.Empty;

            for (var i = caretLineOffset; i >= 0; i--)
            {
               // if ((lineText[i] == '-' || lineText[i] == ' ') && word.Length == 0)
               //     continue;

                if (char.IsLetterOrDigit(lineText[i]) || lineText[i] == '_' || lineText[i] == '$' || lineText[i] == '-')
                {
                    word = lineText[i] + word;
                    continue;
                }

                break;
            }

            return word;
        }

        public bool IsCompletionPosition(int position)
        {
            if (position <= 2)
                return false;

            if (_textArea.Document.Text[position - 1].Equals("$"))
                return false;

            var prevPos = position - 2;
            var prevChar = _textArea.Document.Text[prevPos];

            var line = _textArea.Document.GetLineByOffset(position);
            var offsetToCut = position - line.Offset - 1;

            if (offsetToCut < 1)
                return false;

            var text = _textArea.Document.GetText(line).Substring(0, offsetToCut);

            if (text.Trim().Length == 0)
                return true;

            if (prevChar == '(' || prevChar == '|')
                return true;

            for (var i = position - line.Offset - 2; i >= 0; i--)
            {
                var ch = text[i];

                if (ch == '"' || ch == ')')
                    return false;

                if (ch == '|')
                    return true;
            }

            return false;
            //return prevChar == ' ' || prevChar == '\t' || prevChar == '\n' || prevChar == '(' || prevChar == '|';
        }

        public static bool IsCodeCompletionTrigger(char ch)
        {
            return ch == '-' || ch == '.' || ch == ':' || ch == '$';
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
            if (line == null || lineText == null || _languageContext == null)
                return null;

            if (_languageContext.Tokens.Length >= 2)
            {
                var filteredTokens = _languageContext.Tokens.Where(t => t.Extent.StartLineNumber == line.LineNumber && t.Extent.EndOffset <= position);

                if (filteredTokens.Count() < 1)
                    return null;

                var token = filteredTokens.Last();//[filteredTokens.Count() - 1];

                if (token.TokenFlags == TokenFlags.CommandName)
                    return token;
            }

            return null;
        }
    }
}
