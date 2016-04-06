using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor.Debugging
{
    public class VariableDetails : VariableDetailsBase
    {
        /// <summary>
        /// Provides a constant for the dollar sign variable prefix string.
        /// </summary>
        public const string DollarPrefix = "$";

        private object _valueObject;
        private VariableDetails[] _cachedChildren;

        public VariableDetails(PSVariable psVariable)
            : this(DollarPrefix + psVariable.Name, psVariable.Value)
        {
        }

        public VariableDetails(PSPropertyInfo psProperty)
            : this(psProperty.Name, psProperty.Value)
        {
        }

        public VariableDetails(string name, object value)
        {
            Name = name;
            IsExpandable = GetIsExpandable(value);
            ValueString = GetValueString(value, IsExpandable);
        }

        /// <summary>
        /// If this variable instance is expandable, this method returns the
        /// details of its children.  Otherwise it returns an empty array.
        /// </summary>
        /// <returns></returns>
        public override VariableDetailsBase[] GetChildren()
        {
            VariableDetails[] childVariables = null;

            if (this.IsExpandable)
            {
                if (_cachedChildren == null)
                {
                    _cachedChildren = GetChildren(_valueObject);
                }

                return _cachedChildren;
            }
            else
            {
                childVariables = new VariableDetails[0];
            }

            return childVariables;
        }

        private static VariableDetails[] GetChildren(object obj)
        {
            var childVariables = new List<VariableDetails>();

            if (obj == null)
            {
                return childVariables.ToArray();
            }

            try
            {
                var psObject = obj as PSObject;

                if ((psObject != null) &&
                    (psObject.TypeNames[0] == typeof(PSCustomObject).ToString()))
                {
                    // PowerShell PSCustomObject's properties are completely defined by the ETS type system.
                    childVariables.AddRange(
                        psObject
                            .Properties
                            .Select(p => new VariableDetails(p)));
                }
                else
                {
                    // If a PSObject other than a PSCustomObject, unwrap it.
                    if (psObject != null)
                    {
                        obj = psObject.BaseObject;
                    }

                    var dictionary = obj as IDictionary;
                    var enumerable = obj as IEnumerable;

                    // We're in the realm of regular, unwrapped .NET objects
                    if (dictionary != null)
                    {
                        // Buckle up kids, this is a bit weird.  We could not use the LINQ
                        // operator OfType<DictionaryEntry>.  Even though R# will squiggle the
                        // "foreach" keyword below and offer to convert to a LINQ-expression - DON'T DO IT!
                        // The reason is that LINQ extension methods work with objects of type
                        // IEnumerable.  Objects of type Dictionary<,>, respond to iteration via
                        // IEnumerable by returning KeyValuePair<,> objects.  Unfortunately non-generic 
                        // dictionaries like HashTable return DictionaryEntry objects.
                        // It turns out that iteration via C#'s foreach loop, operates on the variable's
                        // type which in this case is IDictionary.  IDictionary was designed to always
                        // return DictionaryEntry objects upon iteration and the Dictionary<,> implementation
                        // honors that when the object is reintepreted as an IDictionary object.
                        // FYI, a test case for this is to open $PSBoundParameters when debugging a
                        // function that defines parameters and has been passed parameters.  
                        // If you open the $PSBoundParameters variable node in this scenario and see nothing, 
                        // this code is broken.
                        int i = 0;
                        foreach (DictionaryEntry entry in dictionary)
                        {
                            childVariables.Add(
                                new VariableDetails(
                                    "[" + i++ + "]",
                                    entry));
                        }
                    }
                    else if (enumerable != null && !(obj is string))
                    {
                        var i = 0;
                        foreach (var item in enumerable)
                        {
                            childVariables.Add(
                                new VariableDetails(
                                    "[" + i++ + "]",
                                    item));
                        }
                    }

                    AddDotNetProperties(obj, childVariables);
                }
            }
            catch (GetValueInvocationException)
            {
                // This exception occurs when accessing the value of a
                // variable causes a script to be executed.  Right now
                // we aren't loading children on the pipeline thread so
                // this causes an exception to be raised.  In this case,
                // just return an empty list of children.
            }

            return childVariables.ToArray();
        }

        private static void AddDotNetProperties(object obj, List<VariableDetails> childVariables)
        {
            var objectType = obj.GetType();
            var properties =
                objectType.GetProperties(
                    BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                // Don't display indexer properties, it causes an exception anyway.
                if (property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                try
                {
                    childVariables.Add(
                        new VariableDetails(
                            property.Name,
                            property.GetValue(obj)));
                }
                catch (Exception ex)
                {
                    // Some properties can throw exceptions, add the property
                    // name and info about the error.
                    if (ex.GetType() == typeof(TargetInvocationException))
                    {
                        ex = ex.InnerException;
                    }

                    childVariables.Add(
                        new VariableDetails(
                            property.Name,
                            new UnableToRetrievePropertyMessage(
                                "Error retrieving property - " + ex.GetType().Name)));
                }
            }
        }

        private static bool GetIsExpandable(object valueObject)
        {
            if (valueObject == null)
            {
                return false;
            }

            // If a PSObject, unwrap it
            var psobject = valueObject as PSObject;
            if (psobject != null)
            {
                valueObject = psobject.BaseObject;
            }

            Type valueType =
                valueObject != null ?
                    valueObject.GetType() :
                    null;

            return
                valueObject != null &&
                !valueType.IsPrimitive &&
                !valueType.IsEnum && // Enums don't have any properties
                !(valueObject is string) && // Strings get treated as IEnumerables
                !(valueObject is decimal) &&
                !(valueObject is UnableToRetrievePropertyMessage);
        }

        private static string GetValueString(object value, bool isExpandable)
        {
            string valueString;

            if (value == null)
            {
                valueString = "null";
            }
            else if (isExpandable)
            {
                Type objType = value.GetType();

                // Get the "value" for an expandable object.  
                if (value is DictionaryEntry)
                {
                    // For DictionaryEntry - display the key/value as the value.
                    var entry = (DictionaryEntry)value;
                    valueString =
                        string.Format(
                            "[{0}, {1}]",
                            entry.Key,
                            GetValueString(entry.Value, GetIsExpandable(entry.Value)));
                }
                else if (value.ToString().Equals(objType.ToString()))
                {
                    // If the ToString() matches the type name, then display the type 
                    // name in PowerShell format.
                    string shortTypeName = objType.Name;

                    // For arrays and ICollection, display the number of contained items.
                    if (value is Array)
                    {
                        var arr = value as Array;
                        if (arr.Rank == 1)
                        {
                            shortTypeName = InsertDimensionSize(shortTypeName, arr.Length);
                        }
                    }
                    else if (value is ICollection)
                    {
                        var collection = (ICollection)value;
                        shortTypeName = InsertDimensionSize(shortTypeName, collection.Count);
                    }

                    valueString = "[" + shortTypeName + "]";
                }
                else
                {
                    valueString = value.ToString();
                }
            }
            else
            {
                // ToString() output is not the typename, so display that as this object's value
                if (value is string)
                {
                    valueString = "\"" + value + "\"";
                }
                else
                {
                    valueString = value.ToString();
                }
            }

            return valueString;
        }

        private static string InsertDimensionSize(string value, int dimensionSize)
        {
            string result = value;

            int indexLastRBracket = value.LastIndexOf("]");
            if (indexLastRBracket > 0)
            {
                result =
                    value.Substring(0, indexLastRBracket) +
                    dimensionSize +
                    value.Substring(indexLastRBracket);
            }
            else
            {
                // Types like ArrayList don't use [] in type name so
                // display value like so -  [ArrayList: 5]
                result = value + ": " + dimensionSize;
            }

            return result;
        }

        private struct UnableToRetrievePropertyMessage
        {
            public UnableToRetrievePropertyMessage(string message)
            {
                this.Message = message;
            }

            private string Message { get; }

            public override string ToString()
            {
                return "<" + Message + ">";
            }
        }
    }
}
