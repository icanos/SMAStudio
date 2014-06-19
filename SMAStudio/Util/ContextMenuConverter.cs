using SMAStudio.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace SMAStudio.Util
{
    //[ValueConversion(typeof(DocumentReference), typeof(ContextMenu))]
    public class ContextMenuConverter : IValueConverter
    {
        public static ContextMenu DocumentReferenceContextMenu;
        public static ContextMenu DefaultContextMenu;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DocumentReference)
                return DocumentReferenceContextMenu;

            return DefaultContextMenu;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
