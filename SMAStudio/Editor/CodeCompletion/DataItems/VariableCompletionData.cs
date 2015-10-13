using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Editor.CodeCompletion.DataItems
{
    public class VariableCompletionData : CompletionData
    {
        public VariableCompletionData(string variableName)
        {
            DisplayText = variableName;
            CompletionText = variableName;
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
