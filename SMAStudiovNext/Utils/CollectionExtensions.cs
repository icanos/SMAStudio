using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SMAStudiovNext.Utils
{
    public static class CollectionExtensions
    {
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> coll)
        {
            var c = new ObservableCollection<T>();
            foreach (var e in coll)
                c.Add(e);
            return c;
        }

        public static ResourceContainer TreeFind(this IEnumerable<ResourceContainer> coll, ResourceContainer parent, ResourceContainer nodeToFind)
        {
            if (coll == null)
                return null;

            var result = coll.FindElement(nodeToFind);

            if (result == null)
            {
                foreach (var item in coll)
                {
                    result = item.Items.TreeFind(item, nodeToFind);

                    if (result != null)
                        return result;
                }

                if (result == null)
                    return null;
            }

            return parent;
        }
    }
}
