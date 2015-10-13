using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using SMAStudio.Editor.CodeCompletion;
using SMAStudio.Editor.CodeCompletion.DataItems;
using SMAStudio.Language;
using SMAStudio.Resources;
using SMAStudio.Services;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace SMAStudio.Editor
{
    public class CodeTextEditor : TextEditor, IDisposable, INotifyPropertyChanged
    {
        private CompletionWindow _completionWindow;
        private IWorkspaceViewModel _workspaceViewModel;

        public CodeTextEditor()
        {
            TextArea.TextEntering += OnTextEntering;
            TextArea.TextEntered += OnTextEntered;

            Background = (Brush)new BrushConverter().ConvertFrom("#1e1e1e");
            Foreground = Brushes.White;
            ShowLineNumbers = true;

            _workspaceViewModel = Core.Resolve<IWorkspaceViewModel>();

            if (_workspaceViewModel.CurrentDocument is RunbookViewModel)
            {
                ((RunbookViewModel)_workspaceViewModel.CurrentDocument).MvvmTextArea = TextArea;
            }

            #region Load Syntax Highlighting definition
            var dir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SMAStudio.Xshd.SMA.xshd";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (XmlTextReader reader = new XmlTextReader(stream))
            {
                SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);

            }
            #endregion

            Context = new PowershellContext();

            var ctrlSpace = new RoutedCommand();
            ctrlSpace.InputGestures.Add(new KeyGesture(Key.Space, ModifierKeys.Control));
            var cb = new CommandBinding(ctrlSpace, OnCtrlSpaceCommand);

            this.CommandBindings.Add(cb);

            _workspaceViewModel.CurrentDocument.DocumentLoaded();
        }

        public PowershellContext Context { get; set; }

        #region Code Completion Event Handlers
        private void OnTextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && _completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]) && e.Text[0] != '-')
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently element.
                    _completionWindow.CompletionList.RequestInsertion(e);
                }
            }
            //else if (e.Text.Length > 0)
                
        }

        private void OnTextEntered(object sender, TextCompositionEventArgs e)
        {
            var cachedCaretOffset = CaretOffset;
            var cachedText = Text;

            AsyncService.Execute(System.Threading.ThreadPriority.BelowNormal, delegate()
            {
                Context.SetContent(cachedText);

                var contextName = Context.GetContextName(cachedCaretOffset);

                if (contextName == ExpressionType.Parameter ||
                    contextName == ExpressionType.Keyword ||
                    contextName == ExpressionType.Variable)
                {
                    // We need to find the "whole" word before showing auto completion
                    string word = "";

                    for (int i = cachedCaretOffset - 1; i >= 0; i--)
                    {
                        var ch = cachedText[i];

                        if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                            break;

                        word = cachedText[i] + word;
                    }

                    ShowCompletion(cachedCaretOffset, contextName, word, false);
                }
            });
        }

        private void OnCtrlSpaceCommand(object sender, ExecutedRoutedEventArgs e)
        {
            var contextName = Context.GetContextName(CaretOffset);
            ShowCompletion(CaretOffset, contextName, null, true);
        }

        private void ShowCompletion(int cachedCaretOffset, ExpressionType contextName, string text, bool controlSpace)
        {
            if (_completionWindow != null)
                return;

            if (!controlSpace && text.Trim().Length == 0)
                return;

            List<CompletionData> data = new List<CompletionData>();

            // Determine what kind of context we're in so that
            // we can display the correct intellisense
            switch (contextName)
            {
                case ExpressionType.Variable:
                    data.AddRange(Context.GetVariables(cachedCaretOffset, text));
                    break;
                case ExpressionType.Parameter:
                    data.AddRange(Context.GetParameters(cachedCaretOffset, text));
                    break;
                case ExpressionType.Keyword:
                    data.AddRange(Context.GetCmdlets(cachedCaretOffset, text));
                    break;
                case ExpressionType.LanguageConstruct:
                    data.AddRange(Context.GetLanguageConstructs(text));
                    break;
            }

            if (data == null || (data != null && data.Count == 0))
                return;

            AsyncService.ExecuteOnUIThread(delegate()
            {
                _completionWindow = new CompletionWindow(TextArea);
                _completionWindow.CloseAutomatically = true;
                _completionWindow.CloseWhenCaretAtBeginning = true;
                _completionWindow.MinWidth = 260;

                if (text != null)
                    _completionWindow.StartOffset -= text.Length;

                if (controlSpace)
                    _completionWindow.StartOffset = cachedCaretOffset + 1;

                foreach (var item in data)
                    _completionWindow.CompletionList.CompletionData.Add(item);
                /*foreach (var item in data)
                {
                    var completionData = new CompletionData(item);

                    switch (contextName)
                    {
                        case ExpressionType.Variable:
                            completionData.Image = Icons.GetImage(Icons.Variable);
                            break;
                        case ExpressionType.Parameter:
                            completionData.Image = Icons.GetImage(Icons.Tag);
                            break;
                        case ExpressionType.Keyword:
                            completionData.Image = Icons.GetImage(Icons.Runbook);
                            break;
                    }

                    _completionWindow.CompletionList.CompletionData.Add(completionData);
                }*/

                if (text != null)
                    _completionWindow.CompletionList.SelectItem(text);

                _completionWindow.Show();
                _completionWindow.Closed += (o, args) => _completionWindow = null;
            });
        }

        /// <summary>
        /// Gets the document used for code completion, can be overridden to provide a custom document
        /// </summary>
        /// <param name="offset"></param>
        /// <returns>The document of this text editor.</returns>
        protected virtual TextDocument GetCompletionDocument(out int offset)
        {
            offset = CaretOffset;
            return Document;
        }
        #endregion

        #region Dependency Properties
        public static DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(CodeTextEditor),
                // binding changed callback: set value of underlying property
            new PropertyMetadata((obj, args) =>
            {
                CodeTextEditor target = (CodeTextEditor)obj;
                target.Text = (string)args.NewValue;
            })
        );

        public static DependencyProperty CaretOffsetProperty =
            DependencyProperty.Register("CaretOffset", typeof(int), typeof(CodeTextEditor),
                // binding changed callback: set value of underlying property
            new PropertyMetadata((obj, args) =>
            {
                CodeTextEditor target = (CodeTextEditor)obj;
                target.CaretOffset = (int)args.NewValue;
            })
        );
        #endregion

        #region Properties
        public new string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        public new int CaretOffset
        {
            get { return base.CaretOffset; }
            set { base.CaretOffset = value; }
        }
        #endregion

        #region PropertyChangedEventHandler
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        #endregion

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
