using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Util
{
    public static class ListExtension
    {
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> enumerable)
        {
            var col = new ObservableCollection<T>();

            foreach (var cur in enumerable)
            {
                col.Add(cur);
            }
            
            return col;
        }

        public static bool ContainsElement<T>(this IEnumerable<T> list, object obj)
        {
            bool found = false;

            foreach (var elem in list)
            {
                if (elem.Equals(obj))
                {
                    found = true;
                    break;
                }
            }

            return found;
        }
    }
}
