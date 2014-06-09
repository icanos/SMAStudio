using SMAStudio.SMAWebService;
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio
{
    public static class ApiHelpers
    {
        public static string GetJobOutput(OrchestratorApi orchestratorApi, Job job)
        {
            return ApiHelpers.EndGetJobOutput(orchestratorApi, ApiHelpers.BeginGetJobOutput(orchestratorApi, job, null, null));
        }
        
        public static IAsyncResult BeginGetJobOutput(OrchestratorApi orchestratorApi, Job job, AsyncCallback callback, object state)
        {
            if (orchestratorApi == null)
            {
                throw new ArgumentNullException("orchestratorApi");
            }
            if (job == null)
            {
                throw new ArgumentNullException("job");
            }
            return orchestratorApi.BeginGetReadStream(job, new DataServiceRequestArgs(), callback, state);
        }

        public static string EndGetJobOutput(OrchestratorApi orchestratorApi, IAsyncResult asyncResult)
        {
            string result;
            using (DataServiceStreamResponse dataServiceStreamResponse = orchestratorApi.EndGetReadStream(asyncResult))
            {
                using (StreamReader streamReader = new StreamReader(dataServiceStreamResponse.Stream))
                {
                    result = streamReader.ReadToEnd();
                }
            }
            return result;
        }
    }
}
