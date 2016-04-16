using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor.Debugging
{
    public class VariableContainerDetails : VariableDetailsBase
    {
        /// <summary>
        /// Provides a constant for the name of the Global scope.
        /// </summary>
        public const string AutoVariablesName = "Auto";

        /// <summary>
        /// Provides a constant for the name of the Global scope.
        /// </summary>
        public const string GlobalScopeName = "Global";

        /// <summary>
        /// Provides a constant for the name of the Local scope.
        /// </summary>
        public const string LocalScopeName = "Local";

        /// <summary>
        /// Provides a constant for the name of the Script scope.
        /// </summary>
        public const string ScriptScopeName = "Script";

        private readonly List<VariableDetailsBase> _children;

        public VariableContainerDetails(string name)
        {
            Id = 1;
            Name = name;
            _children = new List<VariableDetailsBase>();
        }

        /// <summary>
        /// Gets the collection of child variables.
        /// </summary>
        public List<VariableDetailsBase> Children
        {
            get { return _children; }
        }

        /// <summary>
        /// Returns the details of the variable container's children.  If empty, returns an empty array.
        /// </summary>
        /// <returns></returns>
        public override VariableDetailsBase[] GetChildren()
        {
            return _children.ToArray();
        }
    }
}
