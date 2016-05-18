using SMAStudiovNext.Core;
using SMAStudiovNext.Themes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SMAStudiovNext.Utils;

namespace SMAStudiovNext.Agents
{
    public class ThemesAgent : IAgent
    {
        public void Start()
        {
            if (!Directory.Exists(Path.Combine(AppHelper.CachePath, "Themes")))
            {
                Directory.CreateDirectory(Path.Combine(AppHelper.CachePath, "Themes"));
                CreateStandardThemes();

                var themeManager = AppContext.Resolve<IThemeManager>();
                themeManager.LoadThemes();
            }
        }

        public void Stop()
        {
            // Nothing
        }

        private void CreateStandardThemes()
        {
            string githubTheme = "<Theme>\r\n" +
    "\t<Name>Github</Name>\r\n" +
    "\t<Font>Consolas</Font>\r\n" +
    "\t<FontSize>12</FontSize>\r\n" +
    "\t<Background>#ffffff</Background>\r\n" +
    "\t<Foreground>#000000</Foreground>\r\n" +
    "\t<Colors>\r\n" +
        "\t\t<StylePart Expression=\"Keyword\" Italic=\"false\" Bold=\"true\">#000000</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"CommandName\" Italic=\"false\" Bold =\"true\">#990201</StylePart>\r\n" +
        "\t\t<StylePart Expression= \"Comment\" Italic=\"true\">#FF999988</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Operator\">#333333</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"String\">#dd1144</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Identifier\">#000000</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Variable\">#268887</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Parameter\">#990201</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Member\">#000000</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Number\">#2b9999</StylePart>\r\n" +
    "\t</Colors>\r\n" +
"</Theme>";

            string monokaiTheme = "<Theme>\r\n" +
    "\t<Name>Monokai</Name>\r\n" +
    "\t<Font>Consolas</Font>\r\n" +
    "\t<FontSize>12</FontSize>\r\n" +
    "\t<Background>#272720</Background>\r\n" +
    "\t<Foreground>#ffffff</Foreground>\r\n" +
    "\t<Colors>\r\n" +
        "\t\t<StylePart Expression=\"Keyword\" Italic=\"false\" Bold=\"false\">#e4e061</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"CommandName\" Italic=\"false\" Bold=\"true\">#afe800</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Comment\" Italic=\"true\">#74725c</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Operator\">#ffffff</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"String\">#e4e061</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Identifier\">#ffffff</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Variable\">#7ad6f1</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Parameter\">#de2377</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Member\">#ffffff</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Number\">#a777ff</StylePart>\r\n" +
    "\t</Colors>\r\n" +
"</Theme>";

            var textWriter = new StreamWriter(Path.Combine(AppHelper.CachePath, "Themes", "Github.theme"));
            textWriter.Write(githubTheme);
            textWriter.Close();
            
            textWriter = new StreamWriter(Path.Combine(AppHelper.CachePath, "Themes", "Monokai.theme"));
            textWriter.Write(monokaiTheme);
            textWriter.Close();
        }
    }
}
