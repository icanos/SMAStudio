using SMAStudiovNext.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
