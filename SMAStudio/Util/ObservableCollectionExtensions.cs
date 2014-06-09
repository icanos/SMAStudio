using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Util
{
    public static class ObservableCollectionExtensions
    {
        public static ObservableCollection<T> Clone<T>(this ObservableCollection<T> collection)
        {
            var newCollection = new ObservableCollection<T>();

            foreach (var item in collection)
                newCollection.Add(item);

            return newCollection;
        }
    }
}
