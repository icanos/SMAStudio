using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;

namespace SMAStudiovNext.Core.Editor.Inspector
{
    public class ObjectInspectorWindow : CompletionWindowBase
    {
        public ObjectInspectorWindow(TextArea textArea)
            : base(textArea)
        {
            DataContext = this;
        }

        public object SelectedObject { get; set; }
    }
}
