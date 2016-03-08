using System.Collections.Generic;

namespace SMAStudio.Modules.Runbook.Editor.Parser
{
    public enum ExpressionType
    {
        Script,
        Command,
        Parameter,
        String,
        MultilineString,
        MultilineStringStart,
        MultilineStringEnd,
        QuotedString,
        SingleQuotedString,
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
        LanguageConstruct,
        Property,
        Argument,
        Boolean
    }

    public class LanguageSegment
    {
        public LanguageSegment()
        {
        }

        public int Start { get; set; }

        public int Stop { get; set; }

        public int LineNumber { get; set; }

        public ExpressionType Type { get; set; }

        public string Value { get; set; }
    
        public override string ToString()
        {
            return "(" + Start + ":" + Stop + ") " + Type.ToString() + ": " + Value;
            //return Value;
        }
    }
}
