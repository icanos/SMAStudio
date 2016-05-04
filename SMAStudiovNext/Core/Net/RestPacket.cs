using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core.Net
{
    public class RestPacket : Dictionary<string, string>
    {
        public string GetRestString()
        {
            var json = JsonConvert.SerializeObject(this);

            return json;
        }

        public HttpContent GetFormData()
        {
            var keyValuePairs = this.ToList();
            var content = new FormUrlEncodedContent(keyValuePairs);

            return content;
        }
    }
}
