using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Editor.CodeCompletion.DataItems
{
    class VariableCompletionData : CompletionData
    {
        private readonly Token _token;
        private readonly Token _dataTypeToken;

        public VariableCompletionData(Token token, Token dataTypeToken = null)
        {
            _token = token;
            _dataTypeToken = dataTypeToken;

            DisplayText = _token.Text;
            CompletionText = _token.Text;   // remove the $
        }

        private string _description;
        public override string Description
        {
            get
            {
                if (_description == null)
                {
                    _description = (_dataTypeToken != null ? _dataTypeToken.Text + " " : "") + DisplayText;
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
