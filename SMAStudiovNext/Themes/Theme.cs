using Caliburn.Micro;
using Gemini.Modules.Output;
using SMAStudiovNext.Language;
using SMAStudiovNext.Modules.Runbook.Editor.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SMAStudiovNext.Themes
{
    public class Theme
    {
        private StylePart _defaultStyle = null;
        private Dictionary<string, SolidColorBrush> _brushCache = null;

        public Theme()
        {
            _brushCache = new Dictionary<string, SolidColorBrush>();
        }

        public string Name { get; set; }

        public string Font { get; set; }

        public double FontSize { get; set; }

        public string Background { get; set; }

        public string Foreground { get; set; }

        public List<StylePart> Colors { get; set; }
        
        public StylePart GetStyle(TokenKind tokenKind, TokenFlags tokenFlags)
        {
            if (_defaultStyle== null)
            {
                _defaultStyle = new StylePart
                {
                    Color = Foreground,
                    Bold = false,
                    Italic = false
                };
            }

            switch (tokenKind)
            {
                case TokenKind.Comment:
                    return Colors.FirstOrDefault(item => item.Expression == ExpressionType.Comment);
                case TokenKind.And:
                case TokenKind.AndAnd:
                case TokenKind.Bnot:
                case TokenKind.Bor:
                case TokenKind.Band:
                case TokenKind.Bxor:
                case TokenKind.Ceq:
                case TokenKind.Cge:
                case TokenKind.Cgt:
                case TokenKind.Cin:
                case TokenKind.Cle:
                case TokenKind.Clike:
                case TokenKind.Clt:
                case TokenKind.Cmatch:
                case TokenKind.Cne:
                case TokenKind.Cnotcontains:
                case TokenKind.Cnotin:
                case TokenKind.Cnotlike:
                case TokenKind.Cnotmatch:
                case TokenKind.Icontains:
                case TokenKind.Ieq:
                case TokenKind.Ige:
                case TokenKind.Igt:
                case TokenKind.Iin:
                case TokenKind.Ile:
                case TokenKind.Ilike:
                case TokenKind.Ilt:
                case TokenKind.Imatch:
                case TokenKind.Ine:
                case TokenKind.Inotcontains:
                case TokenKind.Inotin:
                case TokenKind.Inotlike:
                case TokenKind.Inotmatch:
                case TokenKind.Ireplace:
                case TokenKind.Is:
                case TokenKind.IsNot:
                case TokenKind.Isplit:
                    return Colors.FirstOrDefault(item => item.Expression == ExpressionType.Operator);
                case TokenKind.HereStringExpandable:
                case TokenKind.HereStringLiteral:
                case TokenKind.StringExpandable:
                case TokenKind.StringLiteral:
                    return Colors.FirstOrDefault(item => item.Expression == ExpressionType.String);
                case TokenKind.Variable:
                    return Colors.FirstOrDefault(item => item.Expression == ExpressionType.Variable);
                case TokenKind.Parameter:
                    return Colors.FirstOrDefault(item => item.Expression == ExpressionType.Parameter);
                case TokenKind.Begin:
                case TokenKind.Break:
                case TokenKind.Catch:
                case TokenKind.Class:
                case TokenKind.Continue:
                case TokenKind.Data:
                case TokenKind.Do:
                case TokenKind.Dynamicparam:
                case TokenKind.Else:
                case TokenKind.ElseIf:
                case TokenKind.End:
                case TokenKind.Exit:
                case TokenKind.Finally:
                case TokenKind.For:
                case TokenKind.Foreach:
                case TokenKind.Function:
                case TokenKind.If:
                case TokenKind.InlineScript:
                case TokenKind.Parallel:
                case TokenKind.Param:
                case TokenKind.Process:
                case TokenKind.Return:
                case TokenKind.Switch:
                case TokenKind.Throw:
                case TokenKind.Trap:
                case TokenKind.Try:
                case TokenKind.Type:
                case TokenKind.Until:
                case TokenKind.While:
                case TokenKind.Workflow:
                    return Colors.FirstOrDefault(item => item.Expression == ExpressionType.Keyword);
                case TokenKind.AtCurly:
                case TokenKind.AtParen:
                case TokenKind.Colon:
                case TokenKind.ColonColon:
                case TokenKind.Comma:
                case TokenKind.Define:
                case TokenKind.Divide:
                case TokenKind.DivideEquals:
                case TokenKind.DollarParen:
                case TokenKind.Dot:
                case TokenKind.DotDot:
                case TokenKind.Equals:
                case TokenKind.LBracket:
                case TokenKind.LCurly:
                case TokenKind.LineContinuation:
                case TokenKind.LParen:
                case TokenKind.Minus:
                case TokenKind.MinusEquals:
                case TokenKind.MinusMinus:
                case TokenKind.Multiply:
                case TokenKind.MultiplyEquals:
                case TokenKind.Pipe:
                case TokenKind.Plus:
                case TokenKind.PlusEquals:
                case TokenKind.PlusPlus:
                case TokenKind.PostfixMinusMinus:
                case TokenKind.PostfixPlusPlus:
                case TokenKind.RBracket:
                case TokenKind.RCurly:
                case TokenKind.RemainderEquals:
                case TokenKind.RParen:
                case TokenKind.Semi:
                    return _defaultStyle;
                case TokenKind.Identifier:
                    if ((tokenFlags & TokenFlags.AttributeName) == TokenFlags.AttributeName)
                        return Colors.FirstOrDefault(item => item.Expression == ExpressionType.Keyword);
                    else if ((tokenFlags & TokenFlags.TypeName) == TokenFlags.TypeName)
                        return Colors.FirstOrDefault(item => item.Expression == ExpressionType.Keyword);
                    else if ((tokenFlags & TokenFlags.Keyword) == TokenFlags.Keyword)
                        return Colors.FirstOrDefault(item => item.Expression == ExpressionType.Keyword);

                    return _defaultStyle;
                case TokenKind.Generic:
                    if ((tokenFlags & TokenFlags.CommandName) == TokenFlags.CommandName)
                        return Colors.FirstOrDefault(item => item.Expression == ExpressionType.CommandName);

                    return _defaultStyle;
            }
            
            return null;
        }

        public SolidColorBrush GetBrush(StylePart style)
        {
            if (style == null)
                return Brushes.Black;

            var hexColor = style.Color;

            if (_brushCache.ContainsKey(hexColor))
                return _brushCache[hexColor];

            var brush = (SolidColorBrush)(new BrushConverter().ConvertFrom(hexColor));
            _brushCache.Add(hexColor, brush);

            return brush;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
