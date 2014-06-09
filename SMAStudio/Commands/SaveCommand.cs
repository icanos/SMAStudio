using SMAStudio.Util;
using SMAStudio.SMAWebService;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Data.Services.Client;

namespace SMAStudio.Commands
{
    public class SaveCommand : ICommand
    {
        private ApiService _api;
        private ComponentsViewModel _componentsViewModel;

        public SaveCommand(ComponentsViewModel componentsViewModel)
        {
            _api = new ApiService();
            _componentsViewModel = componentsViewModel;
        }

        public bool CanExecute(object parameter)
        {
            if (parameter == null)
                return false;

            var document = ((IDocumentViewModel)parameter);

            if (!document.UnsavedChanges)
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

            if (parameter is RunbookViewModel)
                SaveRunbook((RunbookViewModel)parameter);
            else if (parameter is VariableViewModel)
                SaveVariable((VariableViewModel)parameter);
            else if (parameter is CredentialViewModel)
                SaveCredential((CredentialViewModel)parameter);
        }

        private void SaveRunbook(RunbookViewModel rb)
        {
            if (String.IsNullOrEmpty(rb.RunbookName))
            {
                var window = new NewRunbookDialog();
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                window.Topmost = true;
                if (!(bool)window.ShowDialog())
                {
                    // The user canceled the save
                    return;
                }

                SaveNewRunbook(rb);
                return;
            }

            var runbook = _api.Current.Runbooks.Where(r => r.RunbookID == rb.Runbook.RunbookID).FirstOrDefault();
            if (runbook == null)
            {
                MessageBox.Show("The runbook does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!runbook.DraftRunbookVersionID.HasValue || runbook.DraftRunbookVersionID == Guid.Empty)
            {
                MessageBox.Show("The runbook is checked in and can therefore not be edited.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
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

                runbook.Tags = rb.Runbook.Tags;
                runbook.Description = rb.Runbook.Description;

                _api.Current.UpdateObject(runbook);
                _api.Current.SaveChanges();

                rb.CheckedOut = true;
                rb.Runbook = runbook;

                rb.UnsavedChanges = false;
            }
            catch (Exception e)
            {
                Core.Log.Error("Unable to save a draft of the runbook.", e);
                MessageBox.Show("Something went wrong when trying to save the draft. Refer to the logs for more information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void SaveNewRunbook(RunbookViewModel runbookViewModel)
        {
            var runbookVersion = new RunbookVersion
            {
                TenantID = new Guid("00000000-0000-0000-0000-000000000000"),
                IsDraft = true
            };

            _api.Current.AddToRunbookVersions(runbookVersion);

            MemoryStream ms = new MemoryStream();
            byte[] bytes = Encoding.UTF8.GetBytes(runbookViewModel.Content);
            ms.Write(bytes, 0, bytes.Length);
            ms.Seek(0, SeekOrigin.Begin);

            Stream baseStream = (Stream)ms;

            _api.Current.SetSaveStream(runbookVersion, baseStream, true, "application/octet-stream", string.Empty);

            EntityDescriptor ed = null;
            try
            {
                ChangeOperationResponse cor =
                    (ChangeOperationResponse)_api.Current.SaveChanges().FirstOrDefault<OperationResponse>();

                if (cor != null)
                {
                    ed = (cor.Descriptor as EntityDescriptor);
                }
            }
            catch (Exception e)
            {
                Core.Log.Error("Unable to verify the saved runbook.", e);
            }

            if (ed != null && ed.EditLink != null)
            {
                MergeOption mergeOption = _api.Current.MergeOption;
                _api.Current.MergeOption = MergeOption.OverwriteChanges;
                try
                {
                    _api.Current.Execute<RunbookVersion>(ed.EditLink).Count<RunbookVersion>();
                }
                catch (Exception e)
                {
                    Core.Log.Error("Unable to save the runbook.", e);
                    MessageBox.Show("There was an error when saving the runbook. Please try again later.", "Error");
                    return;
                }
                finally
                {
                    _api.Current.MergeOption = mergeOption;
                }
            }

            var runbook = _api.Current.Runbooks.Where(r => r.RunbookID == runbookVersion.RunbookID).FirstOrDefault();

            if (runbook == null)
            {
                // there was some error when importing the runbook
                MessageBox.Show("There was an error when saving the runbook. Please refer to the log for more information.", "Error");
                return;
            }

            // If we have specified any tags for this runbook - we need to save them as well
            if (!String.IsNullOrEmpty(runbookViewModel.Tags))
            {
                runbook.Tags = runbookViewModel.Tags;
                _api.Current.UpdateObject(runbook);

                _api.Current.SaveChanges();
            }

            runbookViewModel.Runbook = runbook;
            runbookViewModel.CheckedOut = true;
            runbookViewModel.UnsavedChanges = false;

            if (!_componentsViewModel.Runbooks.Contains(runbookViewModel))
                _componentsViewModel.AddRunbook(runbookViewModel);
        }

        private void SaveVariable(VariableViewModel variable)
        {
            Variable vari = null;

            if (variable.Variable.VariableID != Guid.Empty)
            {
                vari = _api.Current.Variables.Where(v => v.VariableID.Equals(variable.Variable.VariableID)).FirstOrDefault();

                if (vari == null)
                    return;

                if (vari.IsEncrypted != variable.IsEncrypted)
                {
                    MessageBox.Show("You cannot change encryption status of a variable.", "Error");
                    return;
                }

                vari.Name = variable.Variable.Name;
                vari.Value = variable.Variable.Value;

                _api.Current.UpdateObject(variable.Variable);
                _api.Current.SaveChanges();
            }
            else
            {
                vari = new Variable();

                vari.Name = variable.Name;
                vari.Value = variable.Content;
                vari.IsEncrypted = variable.IsEncrypted;

                if (vari.IsEncrypted)
                {
                    vari.Value = JsonConverter.ToJson(variable.Content);
                }

                _api.Current.AddToVariables(vari);
                _api.Current.SaveChanges();

                variable.Variable = vari;
            }

            variable.UnsavedChanges = false;
            variable.CachedChanges = false;

            _componentsViewModel.AddVariable(variable);
        }

        private void SaveCredential(CredentialViewModel credential)
        {
            Credential cred = null;

            if (credential.Credential.CredentialID != Guid.Empty)
            {
                cred = _api.Current.Credentials.Where(c => c.CredentialID == credential.ID).FirstOrDefault();

                if (cred == null)
                    return;

                cred.Name = credential.Name;
                cred.UserName = credential.Username;
                cred.RawValue = credential.Password;

                _api.Current.UpdateObject(cred);
                _api.Current.SaveChanges();
            }
            else
            {
                cred = new Credential();

                cred.Name = credential.Name;
                cred.UserName = credential.Username;
                cred.RawValue = credential.Password;

                _api.Current.AddToCredentials(cred);
                _api.Current.SaveChanges();

                credential.Credential = cred;
            }

            credential.UnsavedChanges = false;
            credential.CachedChanges = false;

            if (!_componentsViewModel.Credentials.Contains(credential))
                _componentsViewModel.Credentials.Add(credential);
        }
    }
}
