using SMAStudiovNext.SMA;
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core
{
    public static class ActionExtensions
    {
        private const string HttpPost = "POST";
        private const string JobParameterName = "parameters";
        private const string ScheduleIdParameterName = "scheduleId";
        private const string LabelParameterName = "label";
        private static readonly HashSet<string> JobActions = new HashSet<string>
        {
            "Resume",
            "Suspend",
            "Stop"
        };
        private static readonly HashSet<string> RunbookActions = new HashSet<string>
        {
            "Start",
            "StartOnSchedule",
            "GetStatistics",
            "Publish",
            "Edit",
            "Test"
        };
        public static void Resume(this Job job, OrchestratorApi orchestratorApi, string label = null)
        {
            job.EndResume(orchestratorApi, job.BeginResume(orchestratorApi, null, null, label));
        }
        public static IAsyncResult BeginResume(this Job job, OrchestratorApi orchestratorApi, AsyncCallback callback, object state, string label = null)
        {
            return ActionExtensions.BeginJobAction(job, orchestratorApi, "Resume", callback, state, new OperationParameter[]
            {
                new BodyOperationParameter("label", label)
            });
        }
        public static void EndResume(this Job job, OrchestratorApi orchestratorApi, IAsyncResult asyncResult)
        {
            ActionExtensions.EndJobAction(orchestratorApi, asyncResult);
        }
        public static Statistics GetStatistics(this Runbook runbook, OrchestratorApi orchestratorApi, List<NameValuePair> jobParameters = null)
        {
            return runbook.EndGetStatistics(orchestratorApi, runbook.BeginGetStatistics(orchestratorApi, null, null, jobParameters));
        }
        public static IAsyncResult BeginGetStatistics(this Runbook runbook, OrchestratorApi orchestratorApi, AsyncCallback callback, object state, List<NameValuePair> jobParameters = null)
        {
            return ActionExtensions.BeginRunbookAction<Statistics>(runbook, orchestratorApi, "GetStatistics", callback, state, new OperationParameter[]
            {
                new BodyOperationParameter("parameters", jobParameters)
            });
        }
        public static Statistics EndGetStatistics(this Runbook runbook, OrchestratorApi orchestratorApi, IAsyncResult asyncResult)
        {
            return ActionExtensions.EndRunbookAction<Statistics>(orchestratorApi, asyncResult);
        }
        public static Guid StartRunbook(this Runbook runbook, OrchestratorApi orchestratorApi, List<NameValuePair> jobParameters = null)
        {
            return runbook.EndStartRunbook(orchestratorApi, runbook.BeginStartRunbook(orchestratorApi, null, null, jobParameters));
        }
        public static IAsyncResult BeginStartRunbook(this Runbook runbook, OrchestratorApi orchestratorApi, AsyncCallback callback, object state, List<NameValuePair> jobParameters = null)
        {
            return ActionExtensions.BeginRunbookAction<Guid>(runbook, orchestratorApi, "Start", callback, state, new OperationParameter[]
            {
                new BodyOperationParameter("parameters", jobParameters)
            });
        }
        public static Guid EndStartRunbook(this Runbook runbook, OrchestratorApi orchestratorApi, IAsyncResult asyncResult)
        {
            return ActionExtensions.EndRunbookAction<Guid>(orchestratorApi, asyncResult);
        }
        public static Guid StartOnSchedule(this Runbook runbook, OrchestratorApi orchestratorApi, Schedule schedule, List<NameValuePair> jobParameters = null)
        {
            return runbook.EndStartOnSchedule(orchestratorApi, runbook.BeginStartOnSchedule(orchestratorApi, null, null, schedule, jobParameters));
        }
        public static IAsyncResult BeginStartOnSchedule(this Runbook runbook, OrchestratorApi orchestratorApi, AsyncCallback callback, object state, Schedule schedule, List<NameValuePair> jobParameters = null)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException("schedule");
            }
            if (schedule.ScheduleID == Guid.Empty)
            {
                orchestratorApi.AddToSchedules(schedule);
                orchestratorApi.SaveChanges();
            }
            return ActionExtensions.BeginRunbookAction<Guid>(runbook, orchestratorApi, "StartOnSchedule", callback, state, new OperationParameter[]
            {
                new BodyOperationParameter("parameters", jobParameters),
                new BodyOperationParameter("scheduleId", schedule.ScheduleID)
            });
        }
        public static Guid EndStartOnSchedule(this Runbook runbook, OrchestratorApi orchestratorApi, IAsyncResult asyncResult)
        {
            return ActionExtensions.EndRunbookAction<Guid>(orchestratorApi, asyncResult);
        }
        public static void Stop(this Job job, OrchestratorApi orchestratorApi)
        {
            job.EndStop(orchestratorApi, job.BeginStop(orchestratorApi, null, null));
        }
        public static IAsyncResult BeginStop(this Job job, OrchestratorApi orchestratorApi, AsyncCallback callback, object state)
        {
            return ActionExtensions.BeginJobAction(job, orchestratorApi, "Stop", callback, state, new OperationParameter[0]);
        }
        public static void EndStop(this Job job, OrchestratorApi orchestratorApi, IAsyncResult asyncResult)
        {
            ActionExtensions.EndJobAction(orchestratorApi, asyncResult);
        }
        public static void Suspend(this Job job, OrchestratorApi orchestratorApi)
        {
            job.EndSuspend(orchestratorApi, job.BeginSuspend(orchestratorApi, null, null));
        }
        public static IAsyncResult BeginSuspend(this Job job, OrchestratorApi orchestratorApi, AsyncCallback callback, object state)
        {
            return ActionExtensions.BeginJobAction(job, orchestratorApi, "Suspend", callback, state, new OperationParameter[0]);
        }
        public static void EndSuspend(this Job job, OrchestratorApi orchestratorApi, IAsyncResult asyncResult)
        {
            ActionExtensions.EndJobAction(orchestratorApi, asyncResult);
        }
        public static Guid Publish(this Runbook runbook, OrchestratorApi orchestratorApi)
        {
            return runbook.EndPublish(orchestratorApi, runbook.BeginPublish(orchestratorApi, null, null));
        }
        public static IAsyncResult BeginPublish(this Runbook runbook, OrchestratorApi orchestratorApi, AsyncCallback callback, object state)
        {
            return ActionExtensions.BeginRunbookAction<Guid>(runbook, orchestratorApi, "Publish", callback, state, null);
        }
        public static Guid EndPublish(this Runbook runbook, OrchestratorApi orchestratorApi, IAsyncResult asyncResult)
        {
            return ActionExtensions.EndRunbookAction<Guid>(orchestratorApi, asyncResult);
        }
        public static Guid Edit(this Runbook runbook, OrchestratorApi orchestratorApi)
        {
            return runbook.EndEdit(orchestratorApi, runbook.BeginEdit(orchestratorApi, null, null));
        }
        public static IAsyncResult BeginEdit(this Runbook runbook, OrchestratorApi orchestratorApi, AsyncCallback callback, object state)
        {
            return ActionExtensions.BeginRunbookAction<Guid>(runbook, orchestratorApi, "Edit", callback, state, null);
        }
        public static Guid EndEdit(this Runbook runbook, OrchestratorApi orchestratorApi, IAsyncResult asyncResult)
        {
            return ActionExtensions.EndRunbookAction<Guid>(orchestratorApi, asyncResult);
        }
        public static Guid TestRunbook(this Runbook runbook, OrchestratorApi orchestratorApi, List<NameValuePair> jobParameters = null)
        {
            return runbook.EndTestRunbook(orchestratorApi, runbook.BeginTestRunbook(orchestratorApi, null, null, jobParameters));
        }
        public static IAsyncResult BeginTestRunbook(this Runbook runbook, OrchestratorApi orchestratorApi, AsyncCallback callback, object state, List<NameValuePair> jobParameters = null)
        {
            return ActionExtensions.BeginRunbookAction<Guid>(runbook, orchestratorApi, "Test", callback, state, new OperationParameter[]
            {
                new BodyOperationParameter("parameters", jobParameters)
            });
        }
        public static Guid EndTestRunbook(this Runbook runbook, OrchestratorApi orchestratorApi, IAsyncResult asyncResult)
        {
            return ActionExtensions.EndRunbookAction<Guid>(orchestratorApi, asyncResult);
        }
        internal static IEnumerable<TElement> EndExecute<TElement>(DataServiceContext serviceContext, IAsyncResult asyncResult)
        {
            IEnumerable<TElement> result;
            try
            {
                IEnumerable<TElement> enumerable = serviceContext.EndExecute<TElement>(asyncResult);
                QueryOperationResponse<TElement> queryOperationResponse = enumerable as QueryOperationResponse<TElement>;
                if (queryOperationResponse != null && queryOperationResponse.Error != null)
                {
                    throw queryOperationResponse.Error;
                }
                result = enumerable;
            }
            catch (DataServiceQueryException)
            {
                throw;
            }
            finally
            {
                if (asyncResult is IDisposable)
                {
                    (asyncResult as IDisposable).Dispose();
                }
            }

            return result;
        }
        private static IAsyncResult BeginRunbookAction<TElement>(Runbook runbook, OrchestratorApi orchestratorApi, string action, AsyncCallback callback, object state, params OperationParameter[] operationParameters)
        {
            if (runbook == null || runbook.RunbookID == Guid.Empty)
            {
                throw new ArgumentNullException("runbook");
            }
            if (orchestratorApi == null)
            {
                throw new ArgumentNullException("orchestratorApi");
            }
            if (!ActionExtensions.RunbookActions.Contains(action))
            {
                throw new ArgumentOutOfRangeException("action", action, "An invalid Runbook action was requested.");
            }
            return orchestratorApi.BeginExecute<TElement>(ActionExtensions.GetActionUri(orchestratorApi.Runbooks.RequestUri, runbook.RunbookID, action), callback, state, "POST", true, operationParameters);
        }
        private static TElement EndRunbookAction<TElement>(OrchestratorApi orchestratorApi, IAsyncResult asyncResult)
        {
            QueryOperationResponse<TElement> queryOperationResponse = ActionExtensions.EndExecute<TElement>(orchestratorApi, asyncResult) as QueryOperationResponse<TElement>;
            if (queryOperationResponse == null)
            {
                string message = string.Format("The {0} action did not return the expected response type: QueryOperationResponse<{1}>", new StackFrame(1).GetMethod().Name, typeof(TElement).Name);
                throw new InvalidOperationException(message);
            }
            return queryOperationResponse.Single<TElement>();
        }
        private static IAsyncResult BeginJobAction(Job job, OrchestratorApi orchestratorApi, string action, AsyncCallback callback, object state, params OperationParameter[] operationParameters)
        {
            if (job == null || job.JobID == Guid.Empty)
            {
                throw new ArgumentNullException("job");
            }
            if (orchestratorApi == null)
            {
                throw new ArgumentNullException("orchestratorApi");
            }
            if (!ActionExtensions.JobActions.Contains(action))
            {
                throw new ArgumentOutOfRangeException("action", action, "An invalid job action was requested.");
            }
            return orchestratorApi.BeginExecute<Guid>(ActionExtensions.GetActionUri(orchestratorApi.Jobs.RequestUri, job.JobID, action), callback, state, "POST", true, operationParameters);
        }
        private static void EndJobAction(OrchestratorApi orchestratorApi, IAsyncResult asyncResult)
        {
            if (!(ActionExtensions.EndExecute<Guid>(orchestratorApi, asyncResult) is QueryOperationResponse<Guid>))
            {
                string message = string.Format("The {0} action did not return the expected response type: QueryOperationResponse<{1}>", new StackFrame(1).GetMethod().Name, typeof(Guid).Name);
                throw new InvalidOperationException(message);
            }
        }
        private static Uri GetActionUri(Uri requestUri, Guid id, string action)
        {
            UriBuilder uriBuilder = new UriBuilder(requestUri);
            UriBuilder expr_08 = uriBuilder;
            expr_08.Path += string.Format("(guid'{0}')/{1}", id, action);
            return uriBuilder.Uri;
        }
    }
}
