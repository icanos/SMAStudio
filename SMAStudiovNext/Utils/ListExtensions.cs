using System.Collections.Generic;
using System.Linq;

namespace SMAStudiovNext.Utils
{
    public static class ListExtensions
    {
        public static bool ContainsElement<T>(this IEnumerable<T> list, object obj)
        {
            bool found = false;
            var tmp = list.ToList();

            lock (tmp)
            {
                foreach (var elem in tmp)
                {
                    if (elem.Equals(obj))
                    {
                        found = true;
                        break;
                    }
                }
            }

            return found;
        }

        public static T FindElement<T>(this IEnumerable<T> list, object obj)
        {
            T found = default(T);
            var tmp = list.ToList();

            lock (tmp)
            {
                foreach (var elem in tmp)
                {
                    if (elem.Equals(obj))
                    {
                        found = elem;
                        break;
                    }
                }
            }

            return found;
        }
    }
}
