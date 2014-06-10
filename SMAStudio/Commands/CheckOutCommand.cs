using SMAStudio.Util;
using SMAStudio.SMAWebService;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SMAStudio.Services;
using SMAStudio.Settings;
using SMAStudio.Logging;

namespace SMAStudio.Commands
{
    public class CheckOutCommand : ICommand
    {
        private ApiService _api;
        private RunbookService _runbookService;
        private ILoggingService _log;

        public CheckOutCommand()
        {
            _api = new ApiService();
            _runbookService = new RunbookService();
            _log = new log4netLoggingService();
        }

        public bool CanExecute(object parameter)
        {
            if (parameter == null)
                return false;

            if (!(parameter is RunbookViewModel))
                return false;

            var runbook = ((RunbookViewModel)parameter);

            if (!runbook.Runbook.PublishedRunbookVersionID.HasValue)
                return false;

            if (runbook.CheckedOut)
                return false;

            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            if (parameter == null)
                return;

            if (MessageBox.Show("Do you want to check out the runbook?\r\nThis will still allow the current version of the runbook to run.", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var rb = ((RunbookViewModel)parameter);

            var runbook = _api.Current.Runbooks.Where(r => r.RunbookID == rb.Runbook.RunbookID).FirstOrDefault();
            if (runbook == null)
            {
                MessageBox.Show("The runbook does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!runbook.DraftRunbookVersionID.HasValue || runbook.DraftRunbookVersionID == Guid.Empty)
            {
                runbook.DraftRunbookVersionID = new Guid?(runbook.Edit(_api.Current));
            }
            else
            {
                // TODO: Support overwriting of already checked out runbook?
                _log.ErrorFormat("The runbook was already checked out.");
                MessageBox.Show("The runbook's already checked out.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // First, we need to download the published code and then republish it as a draft
            // Retrieve the raw content of the runbook
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(SettingsManager.Current.Settings.SmaWebServiceUrl + "/Runbooks(guid'" + runbook.RunbookID + "')/PublishedRunbookVersion/$value");
            request.Credentials = CredentialCache.DefaultCredentials;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            TextReader reader = new StreamReader(response.GetResponseStream());

            string content = reader.ReadToEnd();

            reader.Close();

            rb.Content = content;

            MemoryStream ms = new MemoryStream();
            byte[] bytes = Encoding.UTF8.GetBytes(rb.Content);
            ms.Write(bytes, 0, bytes.Length);
            ms.Seek(0, SeekOrigin.Begin);

            Stream baseStream = (Stream)ms;
            RunbookVersion entity = (from rv in _api.Current.RunbookVersions
                                     where (Guid?)rv.RunbookVersionID == runbook.DraftRunbookVersionID
                                     select rv).FirstOrDefault<RunbookVersion>();

            _api.Current.SetSaveStream(entity, baseStream, true, "application/octet-stream", string.Empty);
            _api.Current.SaveChanges();

            rb.CheckedOut = true;
            rb.Runbook = runbook;

            rb.Versions = _runbookService.GetVersions(rb);
        }
    }
}
