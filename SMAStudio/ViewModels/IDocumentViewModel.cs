using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.ViewModels
{
    /// <summary>
    /// Base class for RunbookViewModel and VariableViewModel
    /// </summary>
    public interface IDocumentViewModel
    {
        Guid ID { get; set; }

        /// <summary>
        /// Gets or sets the unsaved changes bool flag
        /// </summary>
        bool UnsavedChanges { get; set; }

        /// <summary>
        /// Gets or sets the cached changes bool flag
        /// </summary>
        bool CachedChanges { get; set; }

        /// <summary>
        /// Gets or sets the checked out bool flag
        /// </summary>
        bool CheckedOut { get; set; }

        /// <summary>
        /// Gets or sets the content of the textbox
        /// </summary>
        string Content { get; set; }

        /// <summary>
        /// Gets or sets the icon of the document
        /// </summary>
        string Icon { get; set; }

        /// <summary>
        /// Contains a value of the last timestamp when a keystroke occured in the textbox
        /// </summary>
        DateTime LastTimeKeyDown { get; set; }

        /// <summary>
        /// Event triggered when the text is updated in the document view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TextChanged(object sender, EventArgs e);
    }
}
