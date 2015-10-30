using SMAStudio.Editor.CodeCompletion.DataItems;
using SMAStudio.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml;
using System.Xml.Serialization;

namespace SMAStudio.Editor.CodeCompletion.DataItems
{
    public enum CmdletTypes
    {
        Builtin,
        Custom
    }

    public class CmdletCompletionData : CompletionData
    {
        private CmdletTypes _cmdletType = CmdletTypes.Custom;
        
        public CmdletCompletionData()
        {
            Parameters = new List<ParameterCompletionData>();
        }

        public CmdletCompletionData(string cmdletName, CmdletTypes cmdletType)
        {
            DisplayText = cmdletName;
            CompletionText = cmdletName;
            _cmdletType = cmdletType;

            Parameters = new List<ParameterCompletionData>();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CmdletCompletionData))
                return false;

            return ((CmdletCompletionData)obj).DisplayText.Equals(DisplayText);
        }

        public override int GetHashCode()
        {
            return DisplayText.GetHashCode();
        }

        public override string ToString()
        {
            return DisplayText;
        }

        public List<ParameterCompletionData> Parameters
        {
            get;
            set;
        }

        [XmlIgnore]
        public override ImageSource Image
        {
            get
            {
                if (_cmdletType == CmdletTypes.Custom)
                    return Icons.GetImage(Icons.Cmdlet);
                else if (_cmdletType == CmdletTypes.Builtin)
                    return Icons.GetImage(Icons.LanguageConstruct);

                return Icons.GetImage(Icons.Cmdlet);
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
                if (_description == null)
                {
                    _description = DisplayText;
                    //_description += Environment.NewLine + XmlDocumentationToText(_entity);
                }

                return _description;
            }
            set
            {
                _description = value;
            }
        }
    }
}
