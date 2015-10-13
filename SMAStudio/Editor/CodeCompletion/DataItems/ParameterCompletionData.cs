using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Editor.CodeCompletion.DataItems
{
    public class ParameterCompletionData : CompletionData
    {
        private string _typeName = string.Empty;
        private string _parameterName = string.Empty;
        private bool _switchParameter = false;

        public ParameterCompletionData()
        {
            DisplayText = "";
        }

        public ParameterCompletionData(string typeName, string parameterName, bool switchParameter)
        {
            _typeName = typeName;
            _parameterName = parameterName;
            _switchParameter = switchParameter;

            if (!_switchParameter && !String.IsNullOrEmpty(_typeName))
                DisplayText = _parameterName + ": " + _typeName;
            else if (_switchParameter && !String.IsNullOrEmpty(_typeName))
                DisplayText = "[switch] " + _parameterName;
            else
                DisplayText = _parameterName;

            if (!_parameterName.StartsWith("-"))
                CompletionText = "-" + _parameterName;
            else
                CompletionText = _parameterName;
        }

        private string _description;
        public override string Description
        {
            get
            {
                if (_description == null)
                {
                    _description = DisplayText;

                    // TODO: Read the help text from either PS Help or comment tags on runbooks
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
