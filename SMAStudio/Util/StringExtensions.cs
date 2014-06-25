using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Util
{
    public static class StringExtensions
    {
        /// <summary>
        /// Counts all occurrences of a specified character
        /// </summary>
        /// <param name="value"></param>
        /// <param name="character">Character to search for</param>
        /// <returns>Number of occurrences</returns>
        public static int OccurrencesOf(this string value, char character)
        {
            int count = 0;

            foreach (var chr in value)
            {
                if (chr == character)
                    count++;
            }

            return count;
        }
    }
}
