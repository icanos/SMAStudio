using SMAStudio.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.ViewModels
{
    public class TagViewModel
    {
        private string _tag;

        public TagViewModel(string tag)
        {
            _tag = tag;
            Runbooks = new ObservableCollection<RunbookViewModel>();

            if (!tag.Equals("(untagged)"))
            IsExpanded = true;
        }

        /// <summary>
        /// Name of the tag
        /// </summary>
        public string Name
        {
            get { return _tag; }
        }

        /// <summary>
        /// Used for databinding the UI, same as Name
        /// </summary>
        public string Title
        {
            get { return Name; }
        }

        /// <summary>
        /// Collection of all runbooks that this tag is "owning".
        /// </summary>
        public ObservableCollection<RunbookViewModel> Runbooks { get; set; }

        /// <summary>
        /// Icon for a Runbook
        /// </summary>
        public string Icon
        {
            get { return Icons.Tag; }
        }

        /// <summary>
        /// Determines if the object is expanded or not
        /// </summary>
        public bool IsExpanded
        {
            get;
            set;
        }
    }
}
