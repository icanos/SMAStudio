using SMAStudio.Models;
using SMAStudio.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.ViewModels
{
    public class AddNewItemViewModel : ObservableObject
    {
        public AddNewItemViewModel()
        {
            Templates = new ObservableCollection<DocumentTemplate>();

            LoadTemplates();
        }

        private void LoadTemplates()
        {
            if (!Directory.Exists(Path.Combine(AppHelper.StartupPath, "templates")))
            {
                Directory.CreateDirectory(Path.Combine(AppHelper.StartupPath, "templates"));

                var textWriter = File.CreateText(Path.Combine(AppHelper.StartupPath, "templates", "Standard Template.ps1"));
                textWriter.WriteLine("#DESCRIPTION: Empty runbook template");
                textWriter.WriteLine("");
                textWriter.WriteLine("workflow <RunbookName> {");
                textWriter.WriteLine("");
                textWriter.WriteLine("}");

                textWriter.Flush();
                textWriter.Close();
            }

            var files = Directory.GetFiles(Path.Combine(AppHelper.StartupPath, "templates"), "*.ps1");

            foreach (var file in files)
            {
                TextReader reader = new StreamReader(file);
                string firstLine = reader.ReadLine();
                reader.Close();

                var template = new DocumentTemplate
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

        public ObservableCollection<DocumentTemplate> Templates { get; set; }
    }
}
