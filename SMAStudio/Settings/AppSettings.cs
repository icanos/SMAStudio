using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SMAStudio.Settings
{
    public class AppSettings
    {
        public string SmaWebServiceUrl { get; set; }

        [XmlIgnore]
        public bool IsConfigured
        {
            get
            {
                if (!String.IsNullOrEmpty(SmaWebServiceUrl))
                    return true;

                return false;
            }
        }
    }
}
