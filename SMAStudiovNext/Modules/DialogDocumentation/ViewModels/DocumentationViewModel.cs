using Caliburn.Micro;
using SMAStudiovNext.Core;
using SMAStudiovNext.Core.Documentation;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace SMAStudiovNext.Modules.DialogDocumentation.ViewModels
{
    public delegate void NoArgumentDelegate(object sender, EventArgs e);

    public class DocumentationViewModel : PropertyChangedBase
    {
        private readonly IBackendContext _backendContext;

        public event NoArgumentDelegate OnFinished;

        public DocumentationViewModel(IBackendContext backendContext)
        {
            _backendContext = backendContext;
            ButtonText = "Start";
        }

        public void SetTemplate(string templatePath)
        {
            TemplatePath = templatePath;
            NotifyOfPropertyChange(() => TemplatePath);
        }

        public void SetDocumentOutputPath(string documentPath)
        {
            DocumentOutputPath = documentPath;
            NotifyOfPropertyChange(() => DocumentOutputPath);
        }

        public void SetDrawingOutputPath(string documentPath)
        {
            DrawingOutputPath = documentPath;
            NotifyOfPropertyChange(() => DrawingOutputPath);
        }

        public void GenerateDocumentation()
        {
            using (var documentation = new DocumentationManager(_backendContext))
            {
                ButtonText = "Generating...";
                NotifyOfPropertyChange(() => ButtonText);

                Task.Run(() =>
                {
                    documentation.Generate(TemplatePath, DocumentOutputPath, DrawingOutputPath);

                    Execute.OnUIThread(() =>
                    {
                        MessageBox.Show("The documentation has been successfully generated.", "Documentation completed", MessageBoxButton.OK);

                        if (OnFinished != null)
                            OnFinished(this, new EventArgs());
                    });
                });
            }
        }

        public string TemplatePath { get; set; }

        public string DocumentOutputPath { get; set; }

        public string DrawingOutputPath { get; set; }

        public string ButtonText { get; set; }
    }
}
