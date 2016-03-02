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
            // TODO: Maybe use real URL encoder instead? ;-)
            return str.Replace(" ", "%20").Replace(".", "%2E").Replace("-", "%2D");
        }
    }
}
