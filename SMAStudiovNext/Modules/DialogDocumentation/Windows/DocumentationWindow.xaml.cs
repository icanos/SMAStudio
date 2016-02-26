using Microsoft.Win32;
using SMAStudiovNext.Core;
using SMAStudiovNext.Modules.DialogDocumentation.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SMAStudiovNext.Modules.DialogDocumentation.Windows
{
    /// <summary>
    /// Interaction logic for DocumentationWindow.xaml
    /// </summary>
    public partial class DocumentationWindow : Window
    {
        public DocumentationWindow(IBackendContext backendContext)
        {
            InitializeComponent();
            DataContext = new DocumentationViewModel(backendContext);

            (DataContext as DocumentationViewModel).OnFinished += delegate ()
            {
                Close();
            };
        }

        private void OnGenerateClicked(object sender, EventArgs e)
        {
            (sender as Button).IsEnabled = false;
            (DataContext as DocumentationViewModel).GenerateDocumentation();
        }

        private void OnLocateClicked(object sender, EventArgs e)
        {
            var fileOpenDialog = new OpenFileDialog();
            fileOpenDialog.Filter = "Word Documents|*.docx";
            fileOpenDialog.Title = "Select Document Template";
            fileOpenDialog.CheckFileExists = true;
            fileOpenDialog.Multiselect = false;

            var result = fileOpenDialog.ShowDialog();

            if (result == null)
                return;

            if (result.Value)
            {
                (DataContext as DocumentationViewModel).SetTemplate(fileOpenDialog.FileName);
            }
        }
    }
}
