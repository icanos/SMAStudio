using Caliburn.Micro;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core.Editor.Completion
{
    public class CompletionUtils
    {
        public static bool IsInCommentArea(int position, TextArea textArea)
        {
            return IsInTokenTypesArea(position, textArea, EdgeTrackingMode.RightEdgeIncluded, PSTokenType.Comment);
        }

        public static bool IsInStringArea(int position, TextArea textArea)
        {
            return IsInTokenTypesArea(position, textArea, EdgeTrackingMode.NoneEdgeIncluded, PSTokenType.String);
        }

        public static bool IsInParameterArea(int position, TextArea textArea)
        {
            return IsInTokenTypesArea(position, textArea, EdgeTrackingMode.NoneEdgeIncluded, PSTokenType.CommandParameter);
        }

        public static bool IsInVariableArea(int position, TextArea textArea)
        {
            return IsInTokenTypesArea(position, textArea, EdgeTrackingMode.NoneEdgeIncluded, PSTokenType.Variable);
        }

        public static bool IsInCommandArea(int position, TextArea textArea)
        {
            return IsInTokenTypesArea(position, textArea, EdgeTrackingMode.NoneEdgeIncluded, PSTokenType.Command);
        }

        public static bool IsCommand(string lineText)
        {
            var result = true;
            var text = lineText.TrimStart();

            if (text.Length == 0)
                return false;

            for (var i = 0; i < text.Length; i++)
            {
                if (!char.IsLetterOrDigit(text[i]) && text[i] != '-')
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        private static bool IsInTokenTypesArea(int position, TextArea textArea, EdgeTrackingMode edgeTrackingMode, params PSTokenType[] selectedPSTokenTypes)
        {
            if (position < 0 || position > textArea.Document.Text.Length)
            {
                return false;
            }

            var scriptToCaret = string.Empty;
            Execute.OnUIThread(() =>
            {
                scriptToCaret = textArea.Document.Text.Substring(0, textArea.Caret.Offset);
            });

            Token[] tokens;
            ParseError[] errors;
            System.Management.Automation.Language.Parser.ParseInput(scriptToCaret, out tokens, out errors);

            if (tokens.Length > 0)
            {
                var filteredTokens = tokens.Where(t => selectedPSTokenTypes.Any(k => PSToken.GetPSTokenType(t) == k)).ToList();

                switch (edgeTrackingMode)
                {
                    case EdgeTrackingMode.NoneEdgeIncluded:
                        foreach (var token in filteredTokens)
                        {
                            if (token.Extent.StartOffset < position && position < token.Extent.EndOffset)
                                return true;

                            if (position <= token.Extent.StartOffset)
                                return false;
                        }
                        break;
                    case EdgeTrackingMode.LeftEdgeIncluded:
                        foreach (var token in filteredTokens)
                        {
                            if (token.Extent.StartOffset <= position && position < token.Extent.EndOffset)
                                return true;

                            if (position < token.Extent.StartOffset)
                                return false;
                        }
                        break;
                    case EdgeTrackingMode.RightEdgeIncluded:
                        foreach (var token in filteredTokens)
                        {
                            if (token.Extent.StartOffset < position && position <= token.Extent.EndOffset)
                                return true;

                            if (position < token.Extent.StartOffset)
                                return false;
                        }
                        break;
                    case EdgeTrackingMode.BothEdgesIncluded:
                        foreach (var token in filteredTokens)
                        {
                            if (token.Extent.StartOffset <= position && position <= token.Extent.EndOffset)
                                return true;

                            if (position < token.Extent.StartOffset)
                                return false;
                        }
                        break;
                }
            }

            return false;
        }
    }
}
