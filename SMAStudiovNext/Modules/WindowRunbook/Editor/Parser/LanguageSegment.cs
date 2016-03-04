using System.Collections.Generic;

namespace SMAStudio.Modules.Runbook.Editor.Parser
{
    public enum ExpressionType
    {
        Script,
        Command,
        Parameter,
        String,
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
        Argument
    }

    public class LanguageSegment
    {
        public LanguageSegment()
        {
            Segments = new List<LanguageSegment>();
        }

        public int Start { get; set; }

        public int Stop { get; set; }

        public int LineNumber { get; set; }

        public ExpressionType Type { get; set; }

        public string Value { get; set; }

        public List<LanguageSegment> Segments { get; set; }
    
        public override string ToString()
        {
            return "(" + Start + ":" + Stop + ") " + Type.ToString() + ": " + Value;
            //return Value;
        }
    }
}
