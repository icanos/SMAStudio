using SMAStudio.SMAWebService;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Services
{
    public interface IRunbookService
    {
        /// <summary>
        /// Retrieves all runbooks from the SMA webservice.
        /// </summary>
        /// <param name="forceDownload">Forces a new download form the webservice instead of using the cache (if exists)</param>
        /// <returns>List of runbooks</returns>
        IList<Runbook> GetRunbooks(bool forceDownload = false);

        /// <summary>
        /// Retrieves the list of available tags
        /// </summary>
        /// <returns></returns>
        IList<string> GetTags();

        /// <summary>
        /// Retrieves a collection of TagViewModels used for the UI
        /// </summary>
        /// <returns></returns>
        ObservableCollection<TagViewModel> GetTagViewModels();

        /// <summary>
        /// Creates View Models of each runbook downloaded from the SMA webservice. Will download the runbooks
        /// if it hasn't been done yet.
        /// </summary>
        /// <param name="forceDownload"></param>
        /// <returns></returns>
        ObservableCollection<RunbookViewModel> GetRunbookViewModels(bool forceDownload = false);

        /// <summary>
        /// Retrieves all versions of the runbook passed in to this method.
        /// </summary>
        /// <param name="runbookViewModel">Runbook to retrieve versions from</param>
        /// <returns>List of versions</returns>
        List<RunbookVersionViewModel> GetVersions(RunbookViewModel runbookViewModel);

        /// <summary>
        /// Get a specific runbook
        /// </summary>
        /// <param name="runbookName">Name of the runbook to retrieve</param>
        /// <returns>Runbook object</returns>
        Runbook GetRunbook(string runbookName);

        /// <summary>
        /// Get a specific runbook
        /// </summary>
        /// <param name="runbookId">ID of the runbook to retrieve</param>
        /// <returns>Runbook object</returns>
        Runbook GetRunbook(Guid runbookId);

        /// <summary>
        /// Create a new runbook
        /// </summary>
        /// <returns>true if successful</returns>
        bool Create();

        /// <summary>
        /// Create a new runbook with a specific name and content
        /// </summary>
        /// <param name="runbookName">Name of the runbook</param>
        /// <param name="runbookContent">Template content</param>
        /// <returns>true if successful</returns>
        bool Create(string runbookName, string runbookContent = "");

        /// <summary>
        /// Updates the runbook
        /// </summary>
        /// <param name="runbook">Runbook to update</param>
        /// <returns>true if successful</returns>
        bool Update(RunbookViewModel runbook);

        /// <summary>
        /// Deletes a runbook
        /// </summary>
        /// <param name="runbook">Runbook to delete</param>
        /// <returns>true if successful</returns>
        bool Delete(RunbookViewModel runbook);

        /// <summary>
        /// Checks in the runbook
        /// </summary>
        /// <param name="runbook">Runbook to check in</param>
        /// <returns>true if successful</returns>
        bool CheckIn(RunbookViewModel runbook);

        /// <summary>
        /// Checks out the runbook
        /// </summary>
        /// <param name="runbook">Runbook to check out</param>
        /// <param name="silentCheckOut">true if no dialog windows should be shown if something happens</param>
        /// <returns>true if successful</returns>
        bool CheckOut(RunbookViewModel runbook, bool silentCheckOut = false);

        /// <summary>
        /// Get the GUID of any suspended jobs for the runbook
        /// </summary>
        /// <param name="runbook">Runbook to get suspended jobs from</param>
        /// <returns>ID of the job</returns>
        Guid GetSuspendedJobs(Runbook runbook);

        /// <summary>
        /// Get the GUID of any active jobs for the runbook
        /// </summary>
        /// <param name="runbook">Runbook to get active jobs from</param>
        /// <returns>ID of the job</returns>
        Guid GetActiveJobs(Runbook runbook);
    }
}
