using SMAStudio.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;

namespace SMAStudio.Editor.CodeCompletion.DataItems
{
    public class VariableCompletionData : CompletionData
    {
        public VariableCompletionData(string variableName)
        {
            DisplayText = variableName;
            CompletionText = variableName;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is VariableCompletionData))
                return false;

            return ((VariableCompletionData)obj).DisplayText.Equals(DisplayText);
        }

        public override int GetHashCode()
        {
            return DisplayText.GetHashCode();
        }

        [XmlIgnore]
        public override ImageSource Image
        {
            get
            {
                return Icons.GetImage(Icons.Variable);
            }
            set
            {

            }
        }

        private string _description;
        public override string Description
        {
            get
            {
                /*if (_description == null)
                {
                    _description = (_dataTypeToken != null ? _dataTypeToken.Text + " " : "") + DisplayText;
                }*/

                return _description;
            }
            set
            {
                _description = value;
            }
        }
    }
}
