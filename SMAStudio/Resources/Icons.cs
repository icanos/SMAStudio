using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SMAStudio.Resources
{
    public class Icons
    {
        public const string Runbook = "Icons/blueprint.png";
        public const string ParseError = "Icons/error.png";
        public const string Variable = "Icons/_Property.png";
        public const string Credential = "Icons/_Keyword.png";
        public const string Tag = "Icons/_Folder.png";
        public const string Schedule = "Icons/calendar.png";
        public const string Property = "Icons/_Resources.png";
        public const string Cmdlet = "Icons/Cmdlet.png";
        public const string LanguageConstruct = "Icons/LanguageConstruct.png";
        public const string Flag = "Icons/Flag.png";

        private static Dictionary<string, ImageSource> _imageCache = new Dictionary<string, ImageSource>();

        public static ImageSource GetImage(string icon)
        {
            if (_imageCache.ContainsKey(icon))
                return _imageCache[icon];

            var bitmap = new BitmapImage(new Uri("/" + icon, UriKind.Relative));

            _imageCache.Add(icon, bitmap);

            return bitmap;
        }
    }
}
