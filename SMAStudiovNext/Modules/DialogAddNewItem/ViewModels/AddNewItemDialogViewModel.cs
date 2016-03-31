using System.Collections.ObjectModel;
using System.IO;
using Caliburn.Micro;
using SMAStudiovNext.Modules.DialogAddNewItem.Models;
using SMAStudiovNext.Utils;

namespace SMAStudiovNext.Modules.DialogAddNewItem.ViewModels
{
    public class AddNewItemDialogViewModel : PropertyChangedBase
    {
        public AddNewItemDialogViewModel()
        {
            Templates = new ObservableCollection<RunbookTemplate>();

            LoadExistingTemplates();
        }

        public void LoadExistingTemplates()
        {
            if (!Directory.Exists(Path.Combine(AppHelper.CachePath, "Templates")))
                CreateInitialTemplates();

            var files = Directory.GetFiles(Path.Combine(AppHelper.CachePath, "Templates"), "*.ps1");

            foreach (var file in files)
            {
                TextReader reader = new StreamReader(file);
                string firstLine = reader.ReadLine();
                reader.Close();

                var template = new RunbookTemplate
                {
                    Name = new FileInfo(file).Name.Replace(".ps1", ""),
                    Path = file
                };

                if (firstLine.StartsWith("#DESCRIPTION"))
                {
                    template.Description = firstLine.Replace("#DESCRIPTION", "");
                    template.Description = template.Description.TrimStart(':');
                    template.Description = template.Description.TrimStart(' ');
                }

                Templates.Add(template);
            }
        }

        private void CreateInitialTemplates()
        {
            Directory.CreateDirectory(Path.Combine(AppHelper.CachePath, "Templates"));

            var textWriter = File.CreateText(Path.Combine(AppHelper.CachePath, "Templates", "Standard Template.ps1"));
            textWriter.WriteLine("#DESCRIPTION: Empty runbook template");
            textWriter.WriteLine("");
            textWriter.WriteLine("workflow ${RunbookName} {");
            textWriter.WriteLine("");
            textWriter.WriteLine("}");

            textWriter.Flush();
            textWriter.Close();
        }

        public ObservableCollection<RunbookTemplate> Templates { get; set; }
    }
}
