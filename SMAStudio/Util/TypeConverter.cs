using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SMAStudio.Util
{
    public class TypeConverter
    {
        /// <summary>
        /// Convert the value from the parameter to correct data type and return
        /// the value in JSON encoded format.
        /// </summary>
        /// <param name="param">Parameter to parse</param>
        /// <returns>JSON encoded data</returns>
        public static object Convert(UIInputParameter param)
        {
            if (String.IsNullOrEmpty(param.Value))
                return string.Empty;

            switch (param.TypeName.ToLower())
            {
                case "int":
                    int value = 0;
                    if (!int.TryParse(param.Value, out value))
                    {
                        return null;
                    }

                    return JsonConverter.ToJson(value);
                case "boolean":
                    bool boolValue = false;
                    if (!bool.TryParse(param.Value, out boolValue))
                    {
                        return null;
                    }

                    return JsonConverter.ToJson(boolValue);
                case "DateTime":
                    DateTime dateValue = DateTime.MinValue;
                    if (!DateTime.TryParse(param.Value, out dateValue))
                    {
                        return null;
                    }

                    return JsonConverter.ToJson(dateValue);
                case "string":
                    return JsonConverter.ToJson(param.Value);
                case "string[]":
                    string[] arrayValue = null;
                    arrayValue = param.Value.Split(',');

                    return JsonConverter.ToJson(arrayValue);
            }

            // Unsupported type
            Core.Log.DebugFormat("Unrecognized type found = " + param.TypeName);
            return null;
        }
    }
}
