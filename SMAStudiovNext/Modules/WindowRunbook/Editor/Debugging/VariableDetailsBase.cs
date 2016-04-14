using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor.Debugging
{
    public abstract class VariableDetailsBase
    {
        /// <summary>
        /// Provides a constant that is used as the starting variable ID for all.
        /// Avoid 0 as it indicates a variable node with no children.
        /// variables.
        /// </summary>
        public const int FirstVariableId = 1;

        public int Id { get; protected set; }

        /// <summary>
        /// Gets the variable's name.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets the string representation of the variable's value.
        /// If the variable is an expandable object, this string
        /// will be empty.
        /// </summary>
        public string ValueString { get; protected set; }

        public object Value { get; protected set; }

        /// <summary>
        /// Returns true if the variable's value is expandable, meaning
        /// that it has child properties or its contents can be enumerated.
        /// </summary>
        public bool IsExpandable { get; protected set; }

        /// <summary>
        /// If this variable instance is expandable, this method returns the
        /// details of its children.  Otherwise it returns an empty array.
        /// </summary>
        /// <returns></returns>
        public abstract VariableDetailsBase[] GetChildren();
    }
}
