using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Debugging;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor.Inspector
{
    public class VariableInspector
    {
        public VariableInspector(VariableDetailsBase variable)
        {
            Name = variable.Name;
            Value = variable.Value;
        }

        public string Name { get; set; }

        [ExpandableObject]
        public object Value { get; set; }
    }
}
