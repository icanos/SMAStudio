using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SMAStudio.Util
{
    public class StringHelper
    {
        public static string FindWordBeforeDash(string text)
        {
            string found = "";

            for (int i = text.Length - 1; i >= 0; i--)
            {
                if (text[i] == ' ')
                    break;

                // We only want to have letters or digit (i think)
                //if (!char.IsLetterOrDigit(text[i]))
                //    continue;

                found = text[i] + found;
            }

            return found;
        }
    }
}
