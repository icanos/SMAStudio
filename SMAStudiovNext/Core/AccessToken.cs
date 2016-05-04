using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core
{
    public class AccessToken
    {
        public string access_token { get; set; }

        public int expires_on { get; set; }

        public DateTime ExpiresOn { get; set; }

        public string refresh_token { get; set; }

        public string resource { get; set; }
    }
}
