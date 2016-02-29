using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Themes
{
    public interface IThemeManager
    {
        event UpdateCurrentThemeDelegate UpdateCurrentTheme;

        void LoadThemes();

        void SetCurrentTheme(Theme theme);

        Theme CurrentTheme { get; }

        IList<Theme> Themes { get; }
    }
}
