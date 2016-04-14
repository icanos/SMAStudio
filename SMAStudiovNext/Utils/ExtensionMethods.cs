using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SMAStudiovNext.Utils
{
    static class ExtensionMethods
    {
        #region ObservableCollections
        public static int Remove<T>(this ObservableCollection<T> coll, Func<T, bool> condition)
        {
            var itemsToRemove = coll.Where(condition).ToList();

            foreach (var itemToRemove in itemsToRemove)
            {
                coll.Remove(itemToRemove);
            }

            return itemsToRemove.Count;
        }
        #endregion

        #region DPI independence
        public static Rect TransformToDevice(this Rect rect, Visual visual)
        {
            Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
            return Rect.Transform(rect, matrix);
        }

        public static Rect TransformFromDevice(this Rect rect, Visual visual)
        {
            Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
            return Rect.Transform(rect, matrix);
        }

        public static Size TransformToDevice(this Size size, Visual visual)
        {
            Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
            return new Size(size.Width * matrix.M11, size.Height * matrix.M22);
        }

        public static Size TransformFromDevice(this Size size, Visual visual)
        {
            Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
            return new Size(size.Width * matrix.M11, size.Height * matrix.M22);
        }

        public static Point TransformToDevice(this Point point, Visual visual)
        {
            Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
            return new Point(point.X * matrix.M11, point.Y * matrix.M22);
        }

        public static Point TransformFromDevice(this Point point, Visual visual)
        {
            Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
            return new Point(point.X * matrix.M11, point.Y * matrix.M22);
        }
        #endregion

        #region System.Drawing <-> WPF conversions
        public static System.Drawing.Point ToSystemDrawing(this Point p)
        {
            return new System.Drawing.Point((int)p.X, (int)p.Y);
        }

        public static Point ToWpf(this System.Drawing.Point p)
        {
            return new Point(p.X, p.Y);
        }

        public static Size ToWpf(this System.Drawing.Size s)
        {
            return new Size(s.Width, s.Height);
        }

        public static Rect ToWpf(this System.Drawing.Rectangle rect)
        {
            return new Rect(rect.Location.ToWpf(), rect.Size.ToWpf());
        }
        #endregion
    }
}
