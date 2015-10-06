using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Language
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
        TypeEnd
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
            return "(" + Start + ":" + Stop + ") " + Type.ToString() + ": " + Value;
        }
    }
}
