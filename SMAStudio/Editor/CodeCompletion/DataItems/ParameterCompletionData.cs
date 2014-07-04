using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Editor.CodeCompletion.DataItems
{
    class ParameterCompletionData : CompletionData
    {
        private readonly ParameterAst _parameter;
        private readonly PropertyInfo _propInfo;

        public ParameterCompletionData(PropertyInfo propInfo, bool includeDash)
        {
            _propInfo = propInfo;
            string parameterName = propInfo.Name;
            string dataType = propInfo.PropertyType.ToString();
            dataType = dataType.Substring(dataType.LastIndexOf('.') + 1);

            if (includeDash)
                DisplayText = "-";

            DisplayText += parameterName;

            if (!dataType.Equals("switchparameter", StringComparison.InvariantCultureIgnoreCase))
                DisplayText += " <" + dataType + ">";

            CompletionText = (includeDash ? "-" : "") + parameterName;
        }

        public ParameterCompletionData(ParameterAst parameter, bool includeDash)
        {
            _parameter = parameter;
            string parameterName = parameter.Name.Extent.Text.Replace("$", "");

            if (includeDash)
                DisplayText = "-";

            DisplayText += parameterName;

            // Add data type
            DisplayText += " <" + parameter.StaticType.Name + ">";

            CompletionText = (includeDash ? "-" : "") + parameterName;
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
