using SMAStudio.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.ViewModels
{
    public interface IErrorListViewModel
    {
        void AddItem(ErrorListItem errorListItem);

        void RemoveItem(ErrorListItem errorListItem);

        void RemoveErrorByRunbook(string runbookName);

        void RemoveFixedErrors(ParseError[] parseErrors, string runbookName);

        ObservableCollection<ErrorListItem> Items { get; set; }
    }
}
