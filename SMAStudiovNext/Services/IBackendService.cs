using SMAStudiovNext.Core;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.Runbook.CodeCompletion;
using SMAStudiovNext.Modules.Runbook.ViewModels;
using SMAStudiovNext.SMA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Services
{
    public interface IBackendService
    {
        /// <summary>
        /// Load objects
        /// </summary>
        void Load();

        /// <summary>
        /// Save the instance of the provided object
        /// </summary>
        void Save(IViewModel instance);
        
        /// <summary>
        /// Publishes as runbook in SMA
        /// </summary>
        /// <param name="runbook"></param>
        /// <returns></returns>
        Task<bool> CheckIn(RunbookModelProxy runbook);

        /// <summary>
        /// Converts Published to Draft and allows you to edit the runbook
        /// </summary>
        /// <param name="runbook"></param>
        /// <returns></returns>
        Task<bool> CheckOut(RunbookViewModel runbook);

        /// <summary>
        /// Checks if the provided runbook has any running/suspended/new or activating jobs.
        /// </summary>
        /// <param name="runbook">Runbook to check</param>
        /// <param name="checkDraft">If we should check the draft or published version</param>
        /// <returns>True/false</returns>
        Task<bool> CheckRunningJobs(RunbookModelProxy runbook, bool checkDraft);

        /// <summary>
        /// Starts a test of a runbook
        /// </summary>
        /// <param name="parameters"></param>
        Guid? TestRunbook(RunbookModelProxy runbookProxy, List<NameValuePair> parameters);

        /// <summary>
        /// Starts execution of a published runbook
        /// </summary>
        /// <param name="runbookVersionId">ID of the runbook version to start</param>
        Guid? StartRunbook(RunbookModelProxy runbookProxy, List<NameValuePair> parameters);

        /// <summary>
        /// Retrieve information about a specific job from the backend service
        /// </summary>
        /// <param name="jobId">Guid to retrieve information about</param>
        /// <returns>Proxy object or null</returns>
        JobModelProxy GetJobDetails(RunbookModelProxy runbooks);

        /// <summary>
        /// Retrieve information about a specific job from the backend service
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        JobModelProxy GetJobDetails(Guid jobId);

        /// <summary>
        /// Retrieves a list of jobs that has been executed for the specified runbook
        /// </summary>
        /// <param name="runbookId">ID to retrieve jobs from</param>
        /// <returns>List of jobs</returns>
        IList<JobModelProxy> GetJobs(Guid runbookVersionId);

        /// <summary>
        /// Pauses execution of a runbook
        /// </summary>
        /// <param name="jobId">ID to pause</param>
        void PauseExecution(Guid jobId);

        /// <summary>
        /// Resumes execution of a runbook
        /// </summary>
        /// <param name="jobId">ID of the job to resume</param>
        void ResumeExecution(Guid jobId);

        /// <summary>
        /// Stops execution of a runbook
        /// </summary>
        /// <param name="jobId">ID of the job to stop</param>
        void StopExecution(Guid jobId);

        /// <summary>
        /// Delete a object from the backend service
        /// </summary>
        /// <param name="model">Object to delete</param>
        /// <returns>True/false</returns>
        bool Delete(ModelProxyBase model);

        /// <summary>
        /// Download content for a specific URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        string GetContent(string url);

        /// <summary>
        /// Returns the URL which the web service responds to
        /// </summary>
        /// <returns></returns>
        string GetBackendUrl(RunbookType runbookType, RunbookModelProxy runbook);

        /// <summary>
        /// Return a list of parameters that is required/optional to start this runbook
        /// </summary>
        /// <param name="runbookViewModel"></param>
        /// <returns></returns>
        IList<ICompletionEntry> GetParameters(RunbookViewModel runbookViewModel, KeywordCompletionData completionData);

        /// <summary>
        /// Context of which this service lives
        /// </summary>
        IBackendContext Context { get; }
    }
}
