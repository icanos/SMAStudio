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
    public class CmdletCompletionData : CompletionData
    {
        public CmdletCompletionData()
        {
            Parameters = new List<ParameterCompletionData>();
        }

        public CmdletCompletionData(string cmdletName)
        {
            DisplayText = cmdletName;
            CompletionText = cmdletName;

            Parameters = new List<ParameterCompletionData>();
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
