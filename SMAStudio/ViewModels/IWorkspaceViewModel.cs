using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SMAStudio.ViewModels
{
    public interface IWorkspaceViewModel
    {
        void Initialize();

        void OpenDocument(IDocumentViewModel document);

        void EditorTextEntered(object sender, TextCompositionEventArgs e);

        void EditorTextEntering(object sender, TextCompositionEventArgs e);

        ObservableCollection<IDocumentViewModel> Documents { get; set; }

        int SelectedIndex { get; set; }

        string WindowTitle { get; set; }

        string StatusBarText { get; set; }

        IDocumentViewModel CurrentDocument { get; }
    }
}
