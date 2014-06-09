using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SMAStudio
{
    /// <summary>
    /// Interaction logic for FindReplaceDialog.xaml
    /// </summary>
    public partial class FindReplaceDialog : Window
    {
        private static string _textToFind = "";
        private static bool _caseSensitive = true;
        private static bool _wholeWord = true;
        private static bool _useRegex = false;
        private static bool _useWildcards = false;
        private static bool _searchUp = false;

        private TextEditor _textEditor;

        public FindReplaceDialog(TextEditor textEditor)
        {
            InitializeComponent();

            _textEditor = textEditor;

            txtFind.Text = txtFind2.Text = _textToFind;
            cbCaseSensitive.IsChecked = _caseSensitive;
            cbWholeWord.IsChecked = _wholeWord;
            cbRegex.IsChecked = _useRegex;
            cbWildcards.IsChecked = _useWildcards;
            cbSearchUp.IsChecked = _searchUp;
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            _textToFind = txtFind2.Text;
            _caseSensitive = (cbCaseSensitive.IsChecked == true);
            _wholeWord = (cbWholeWord.IsChecked == true);
            _useRegex = (cbRegex.IsChecked == true);
            _useWildcards = (cbWildcards.IsChecked == true);
            _searchUp = (cbSearchUp.IsChecked == true);

            _theDialog = null;
        }

        private void FindNextClick(object sender, RoutedEventArgs e)
        {
            if (!FindNext(txtFind.Text))
                SystemSounds.Beep.Play();
        }

        private void FindNext2Click(object sender, RoutedEventArgs e)
        {
            if (!FindNext(txtFind2.Text))
                SystemSounds.Beep.Play();
        }

        private void ReplaceClick(object sender, RoutedEventArgs e)
        {
            Regex regex = GetRegEx(txtFind2.Text);
            string input = _textEditor.Text.Substring(_textEditor.SelectionStart, _textEditor.SelectionLength);
            Match match = regex.Match(input);
            bool replaced = false;
            if (match.Success && match.Index == 0 && match.Length == input.Length)
            {
                _textEditor.Document.Replace(_textEditor.SelectionStart, _textEditor.SelectionLength, txtReplace.Text);
                replaced = true;
            }

            if (!FindNext(txtFind2.Text) && !replaced)
                SystemSounds.Beep.Play();
        }

        private void ReplaceAllClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to Replace All occurences of \"" +
            txtFind2.Text + "\" with \"" + txtReplace.Text + "\"?",
                "Replace All", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                Regex regex = GetRegEx(txtFind2.Text, true);
                int offset = 0;
                _textEditor.BeginChange();
                foreach (Match match in regex.Matches(_textEditor.Text))
                {
                    _textEditor.Document.Replace(offset + match.Index, match.Length, txtReplace.Text);
                    offset += txtReplace.Text.Length - match.Length;
                }
                _textEditor.EndChange();
            }
        }

        private bool FindNext(string textToFind)
        {
            Regex regex = GetRegEx(textToFind);
            int start = regex.Options.HasFlag(RegexOptions.RightToLeft) ?
            _textEditor.SelectionStart : _textEditor.SelectionStart + _textEditor.SelectionLength;
            Match match = regex.Match(_textEditor.Text, start);

            if (!match.Success)  // start again from beginning or end
            {
                if (regex.Options.HasFlag(RegexOptions.RightToLeft))
                    match = regex.Match(_textEditor.Text, _textEditor.Text.Length);
                else
                    match = regex.Match(_textEditor.Text, 0);
            }

            if (match.Success)
            {
                _textEditor.Select(match.Index, match.Length);
                TextLocation loc = _textEditor.Document.GetLocation(match.Index);
                _textEditor.ScrollTo(loc.Line, loc.Column);
            }

            return match.Success;
        }

        private Regex GetRegEx(string textToFind, bool leftToRight = false)
        {
            RegexOptions options = RegexOptions.None;
            if (cbSearchUp.IsChecked == true && !leftToRight)
                options |= RegexOptions.RightToLeft;
            if (cbCaseSensitive.IsChecked == false)
                options |= RegexOptions.IgnoreCase;

            if (cbRegex.IsChecked == true)
            {
                return new Regex(textToFind, options);
            }
            else
            {
                string pattern = Regex.Escape(textToFind);
                if (cbWildcards.IsChecked == true)
                    pattern = pattern.Replace("\\*", ".*").Replace("\\?", ".");
                if (cbWholeWord.IsChecked == true)
                    pattern = "\\b" + pattern + "\\b";
                return new Regex(pattern, options);
            }
        }

        private static FindReplaceDialog _theDialog = null;

        public static void ShowForReplace(TextEditor editor)
        {
            if (_theDialog == null)
            {
                _theDialog = new FindReplaceDialog(editor);
                _theDialog.tabMain.SelectedIndex = 1;
                _theDialog.Show();
                _theDialog.Activate();
            }
            else
            {
                _theDialog.tabMain.SelectedIndex = 1;
                _theDialog.Activate();
            }

            if (!editor.TextArea.Selection.IsMultiline)
            {
                _theDialog.txtFind.Text = _theDialog.txtFind2.Text = editor.TextArea.Selection.GetText();
                _theDialog.txtFind.SelectAll();
                _theDialog.txtFind2.SelectAll();
                _theDialog.txtFind2.Focus();
            }
        }
    }
}
