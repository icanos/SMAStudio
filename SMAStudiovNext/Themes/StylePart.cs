using SMAStudiovNext.Modules.Runbook.Editor.Parser;
using System.Xml.Serialization;

namespace SMAStudiovNext.Themes
{
    public class StylePart
    {
        public StylePart()
        {
            //Types = new List<TokenFlags>();
        }

        [XmlAttribute]
        public ExpressionType Expression { get; set; }
        //public List<TokenFlags> Types { get; set; }

        //public List<TokenKind> Kinds { get; set; }

        [XmlText]
        public string Color { get; set; }

        [XmlAttribute]
        public bool Italic { get; set; }

        [XmlAttribute]
        public bool Bold { get; set; }
    }
}
