using System.Collections.Generic;

namespace SMAStudiovNext.Language
{
    public enum ExpressionType
    {
        Script,
        Command,
        Parameter,
        String,
        QuotedString,
        Integer,
        BlockStart,
        BlockEnd,
        Operator,
        Keyword,
        None,
        ExpressionStart,
        ExpressionEnd,
        Variable,
        Function,
        FunctionCall,
        ImportModule,
        Type,
        TypeStart,
        TypeEnd,
        Comment,
        MultilineComment,
        MultilineCommentStart,
        MultilineCommentEnd,
        LanguageConstruct
    }

    public class PowershellSegment
    {
        public PowershellSegment()
        {
            Segments = new List<PowershellSegment>();
        }

        public int Start { get; set; }

        public int Stop { get; set; }

        public ExpressionType Type { get; set; }

        public string Value { get; set; }

        public List<PowershellSegment> Segments { get; set; }

        public override string ToString()
        {
            //return "(" + Start + ":" + Stop + ") " + Type.ToString() + ": " + Value;
            return Value;
        }
    }
}
