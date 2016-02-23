using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Themes
{
    public interface IThemeManager
    {
        void LoadThemes();

        Theme CurrentTheme { get; }
    }
}
