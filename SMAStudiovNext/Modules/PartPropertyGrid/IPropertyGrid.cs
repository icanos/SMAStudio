using Gemini.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.PropertyGrid
{
    public interface IPropertyGrid : ITool
    {
        object SelectedObject { get; set; }
    }
}
