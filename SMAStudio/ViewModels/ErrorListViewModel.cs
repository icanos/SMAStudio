using SMAStudio.Models;
using SMAStudio.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.ViewModels
{
    public class ErrorListViewModel : ObservableObject, IErrorListViewModel
    {
        public ErrorListViewModel()
        {
            Items = new ObservableCollection<ErrorListItem>();
        }

        /// <summary>
        /// Add a new item to the error list (will not add duplicates of the same message)
        /// </summary>
        /// <param name="errorListItem"></param>
        public void AddItem(ErrorListItem errorListItem)
        {
            var error = Items.Where(i => i.ErrorId.Equals(errorListItem.ErrorId) && i.LineNumber.Equals(errorListItem.LineNumber) && i.Runbook.Equals(errorListItem.Runbook));

            if (error.Count() == 0)
            {
                App.Current.Dispatcher.Invoke((Action)delegate()
                {
                    Items.Add(errorListItem);
                });
            }
            
            base.RaisePropertyChanged("Items");
        }

        /// <summary>
        /// Remove a single error list item
        /// </summary>
        /// <param name="errorListItem"></param>
        public void RemoveItem(ErrorListItem errorListItem)
        {
            Items.Remove(errorListItem);
            base.RaisePropertyChanged("Items");
        }

        /// <summary>
        /// Remove all error list items based on runbook name
        /// </summary>
        /// <param name="runbookName"></param>
        public void RemoveErrorByRunbook(string runbookName)
        {
            var errors = Items.Where(i => i.Runbook.Equals(runbookName, StringComparison.InvariantCultureIgnoreCase)).ToList();

            App.Current.Dispatcher.Invoke((Action)delegate()
            {
                foreach (var error in errors)
                    Items.Remove(error);
            });
            
            base.RaisePropertyChanged("Items");
        }

        /// <summary>
        /// Remove all fixed errors for a specific runbook
        /// </summary>
        /// <param name="parseErrors"></param>
        /// <param name="runbookName"></param>
        public void RemoveFixedErrors(ParseError[] parseErrors, string runbookName)
        {
            ObservableCollection<ErrorListItem> tmp = new ObservableCollection<ErrorListItem>();

            foreach (var item in Items)
            {
                if (!item.Runbook.Equals(runbookName, StringComparison.InvariantCultureIgnoreCase))
                    tmp.Add(item);
                else
                {
                    foreach (var error in parseErrors)
                    {
                        if (item.LineNumber.Equals(error.Extent.StartLineNumber) && item.ErrorId.Equals(error.ErrorId))
                        {
                            tmp.Add(item);
                            break;
                        }
                    }
                }
            }

            Items = tmp;
        }

        /// <summary>
        /// Collection containing error items
        /// </summary>
        public ObservableCollection<ErrorListItem> Items { get; set; }
    }
}
