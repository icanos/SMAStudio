using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core
{
    public static class StringExtensions
    {
        public static string ToUrlSafeString(this string str)
        {
            return str.Replace(" ", "%20");
        }
    }
}
