using SMAStudio.Modules.Runbook.Editor.Parser;
using System.Xml.Serialization;

namespace SMAStudiovNext.Themes
{
    public class StylePart
    {
        [XmlAttribute]
        public ExpressionType Expression { get; set; }

        [XmlText]
        public string Color { get; set; }

        [XmlAttribute]
        public bool Italic { get; set; }

        [XmlAttribute]
        public bool Bold { get; set; }
    }
}
