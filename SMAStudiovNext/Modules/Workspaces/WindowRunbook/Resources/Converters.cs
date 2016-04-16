using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Media;
using SMAStudiovNext.Vendor.GitSharp;

namespace SMAStudiovNext.Modules.WindowRunbook.Resources
{
    /// <summary>
	/// Adjust text block sizes to correspond to each other by adding lines
	/// </summary>
	public class BlockTextConverterA : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var section = value as Diff.Section;
            if (section == null)
                return 0;
            var a_lines = section.EndA - section.BeginA;
            var b_lines = section.EndB - section.BeginB;
            var line_difference = Math.Max(a_lines, b_lines) - a_lines;
            var s = new StringBuilder(Regex.Replace(section.TextA, "\r?\n$", ""));
            if (a_lines == 0)
                line_difference -= 1;
            for (var i = 0; i < line_difference; i++)
                s.AppendLine();
            return s.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Adjust text block sizes to correspond to each other by adding lines
    /// </summary>
    public class BlockTextConverterB : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var section = value as Diff.Section;
            if (section == null)
                return 0;
            var a_lines = section.EndA - section.BeginA;
            var b_lines = section.EndB - section.BeginB;
            var line_difference = Math.Max(a_lines, b_lines) - b_lines;
            var s = new StringBuilder(Regex.Replace(section.TextB, "\r?\n$", ""));
            if (b_lines == 0)
                line_difference -= 1;
            for (var i = 0; i < line_difference; i++)
                s.AppendLine();
            return s.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Calculate block background color
    /// </summary>
    public class BlockColorConverterA : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var section = value as Diff.Section;
            if (section == null)
                return Brushes.Pink; // <-- this shouldn't happen anyway
            switch (section.EditWithRespectToA)
            {
                case Diff.EditType.Deleted:
                    return Brushes.LightSkyBlue;
                case Diff.EditType.Replaced:
                    return Brushes.LightSalmon;
                case Diff.EditType.Inserted:
                    return Brushes.DarkGray;
                case Diff.EditType.Unchanged:
                default:
                    return Brushes.White;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Calculate block background color
    /// </summary>
    public class BlockColorConverterB : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var section = value as Diff.Section;
            if (section == null)
                return Brushes.Pink; // <-- this shouldn't happen anyway

            switch (section.EditWithRespectToA)
            {
                case Diff.EditType.Deleted:
                    return Brushes.DarkGray;
                case Diff.EditType.Replaced:
                    return Brushes.LightSalmon;
                case Diff.EditType.Inserted:
                    return Brushes.LightGreen;
                case Diff.EditType.Unchanged:
                default:
                    return Brushes.White;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
