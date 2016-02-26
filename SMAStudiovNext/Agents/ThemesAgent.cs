using SMAStudiovNext.Core;
using SMAStudiovNext.Themes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    "\t<Font>Consolas</Font>\r\n" +
    "\t<FontSize>12</FontSize>\r\n" +
    "\t<Background>#ffffff</Background>\r\n" +
    "\t<Foreground>#000000</Foreground>\r\n" +
    "\t<Colors>\r\n" +
        "\t\t<StylePart Expression=\"Keyword\" Italic=\"false\" Bold=\"true\">#FF000000</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"String\" Italic=\"false\" Bold=\"false\">#FF990201</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"MultilineComment\" Italic=\"true\">#FF999988</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Comment\" Italic=\"true\">#FF999988</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"QuotedString\">#dd1144</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"SingleQuotedString\">#dd1144</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Variable\">#268887</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"LanguageConstruct\">#000000</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Parameter\">#990201</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Integer\">#2b9999</StylePart>\r\n" +
        "\t\t<StylePart Expression=\"Operator\" Background=\"#272720\">#000000</StylePart> <!-- language.operator -->\r\n" +
        "\t\t<StylePart Expression=\"Type\" Bold=\"true\">#000000</StylePart> <!-- keyword.type -->\r\n" +
    "\t</Colors>\r\n" +
"</Theme>";

            string monokaiTheme = "<Theme>" +
    "\t<Font>Consolas</Font>" +
    "\t<FontSize>12</FontSize>" +
    "\t<Background>#272720</Background>" +
    "\t<Foreground>#ffffff</Foreground>" +
    "\t<Colors>" +
        "\t\t<StylePart Expression=\"Keyword\" Italic=\"false\" Bold=\"true\">#afe800</StylePart>" +
        "\t\t<StylePart Expression=\"String\" Italic=\"false\" Bold=\"false\">#ffffff</StylePart>" +
        "\t\t<StylePart Expression=\"MultilineComment\" Italic=\"true\">#74725c</StylePart>" +
        "\t\t<StylePart Expression=\"Comment\" Italic=\"true\">#74725c</StylePart>" +
        "\t\t<StylePart Expression=\"QuotedString\">#e4e061</StylePart>" +
        "\t\t<StylePart Expression=\"SingleQuotedString\">#e4e061</StylePart>" +
        "\t\t<StylePart Expression=\"Variable\">#7ad6f1</StylePart>" +
        "\t\t<StylePart Expression=\"LanguageConstruct\">#7ad6f1</StylePart>" +
        "\t\t<StylePart Expression=\"Parameter\">#de2377</StylePart>" +
        "\t\t<StylePart Expression=\"Integer\">#a777ff</StylePart>" +
        "\t\t<StylePart Expression=\"Operator\" Background=\"#272720\">#e4e061</StylePart> <!-- language.operator -->" +
        "\t\t<StylePart Expression=\"Type\" Bold=\"true\">#ffc66d</StylePart> <!-- keyword.type -->" +
    "\t</Colors>" +
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
