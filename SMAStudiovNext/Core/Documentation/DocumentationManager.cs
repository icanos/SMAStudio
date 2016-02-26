using Novacode;
using SMAStudio.Language;
using SMAStudiovNext.Models;
using SMAStudiovNext.Modules.Runbook.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMAStudiovNext.Core.Documentation
{
    public class DocumentationManager : IDisposable
    {
        private enum DocumentationType
        {
            Synopsis,
            Description,
            Parameter,
            Notes,
            Author,
            None
        }

        private readonly IBackendContext _backendContext;

        public DocumentationManager(IBackendContext backendContext)
        {
            _backendContext = backendContext;
        }

        public void Dispose()
        {
            
        }

        public void Generate(string templatePath)
        {
            var statusManager = AppContext.Resolve<IStatusManager>();
            var languageContext = new LanguageContext();

            using (var document = DocX.Load(templatePath))
            {
                // Create a new page to start on
                document.InsertSectionPageBreak();

                var runbooks = _backendContext.Runbooks;
                foreach (var runbook in runbooks)
                {
                    var runbookProxy = (runbook.Tag as RunbookModelProxy);
                    statusManager.SetText("Generating documentation for " + runbookProxy.RunbookName);
                    var viewModel = (runbook.Tag as RunbookModelProxy).GetViewModel<RunbookViewModel>();

                    var content = viewModel.GetContent(RunbookType.Published, true);
                    var comment = ParseRunbook(content);

                    if (comment == null)
                        continue;

                    // A runbook is defined in the documentation as
                    /*
                    [TITLE 1: Runbook Name]
                    [SYNOPSIS]

                    [LIST OF PARAMETERS]

                    [DESCRIPTION]

                    Notes
                    [NOTES]
                    */
                    var title = document.InsertParagraph(runbookProxy.RunbookName).FontSize(16).Heading(HeadingType.Heading1);
                    title.StyleName = "Heading 1";
                    var synopsis = document.InsertParagraph(comment.Synopsis);

                    var paramName = document.InsertParagraph("Parameters").FontSize(13).Heading(HeadingType.Heading2);
                    paramName.StyleName = "Heading 2";
                    var table = document.InsertTable(comment.Parameters.Count + 1, 2);
                    table.Design = TableDesign.ColorfulGridAccent1;
                    table.AutoFit = AutoFit.Window;

                    table.Rows[0].Cells[0].Paragraphs[0].InsertText("Parameter");
                    table.Rows[0].Cells[1].Paragraphs[0].InsertText("Description");

                    int row = 1;
                    foreach (var parameter in comment.Parameters)
                    {
                        table.Rows[row].Cells[0].Paragraphs[0].InsertText(parameter.Key);
                        table.Rows[row].Cells[1].Paragraphs[0].InsertText(parameter.Value);

                        row++;
                    }

                    document.InsertParagraph(" ");

                    var details = document.InsertParagraph("Details").FontSize(13).Heading(HeadingType.Heading2);
                    details.StyleName = "Heading 2";
                    var description = document.InsertParagraph(comment.Description);
                    var notes = document.InsertParagraph("Notes").FontSize(13).Heading(HeadingType.Heading2);
                    notes.StyleName = "Heading 2";
                    var notesStr = document.InsertParagraph(comment.Notes);

                    document.InsertSectionPageBreak();
                }

                document.SaveAs("C:\\Users\\westin\\Desktop\\Test.docx");

                statusManager.SetTimeoutText("Documentation has successfully been generated.", 5);
            }
        }

        private DocumentationComment ParseRunbook(string content)
        {
            var lines = content.Split('\n');
            var comment = new List<string>();
            bool inComment = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("<#"))
                    inComment = true;

                if (inComment)
                {
                    //comment += line + "\n";
                    comment.Add(line.TrimEnd());
                }

                if (line.EndsWith("#>"))
                    inComment = false;

                if (!inComment)
                    break;
            }

            if (comment.Count > 0)
            {
                var documentation = ParseComment(comment);

                return documentation;
            }

            return null;
        }

        private DocumentationComment ParseComment(List<string> lines)
        {
            var currentType = DocumentationType.None;
            var previousType = DocumentationType.None;
            var content = new StringBuilder();
            var documentation = new DocumentationComment();

            //foreach (var line in lines)
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                if (line.StartsWith(".SYNOPSIS"))
                {
                    currentType = DocumentationType.Synopsis;
                    continue;
                }
                else if (line.StartsWith(".DESCRIPTION"))
                {
                    currentType = DocumentationType.Description;
                    continue;
                }
                else if (line.StartsWith(".PARAMETER"))
                {
                    currentType = DocumentationType.Parameter;
                }
                else if (line.StartsWith(".NOTES"))
                {
                    currentType = DocumentationType.Notes;
                    continue;
                }
                else if (line.StartsWith("Author:"))
                {
                    currentType = DocumentationType.Author;
                }

                if (currentType != previousType)
                {
                    switch (previousType)
                    {
                        case DocumentationType.Description:
                            documentation.Description = content.ToString();
                            content.Clear();
                            break;
                        case DocumentationType.Notes:
                            documentation.Notes = content.ToString();
                            content.Clear();
                            break;
                        case DocumentationType.Synopsis:
                            documentation.Synopsis = content.ToString();
                            content.Clear();
                            break;
                    }
                }

                switch (currentType)
                {
                    case DocumentationType.Author:
                        documentation.Author = ParseAuthor(line);
                        break;
                    case DocumentationType.Notes:
                    case DocumentationType.Synopsis:
                    case DocumentationType.Description:
                        content.AppendLine(line.Trim());
                        break;
                    case DocumentationType.Parameter:
                        i = ParseParameter(ref documentation, line, lines, i);
                        currentType = DocumentationType.None;
                        break;
                }

                previousType = currentType;
            }

            if (content.Length > 0)
            {
                switch(currentType)
                {
                    case DocumentationType.Description:
                        documentation.Description = content.ToString();
                        break;
                    case DocumentationType.Notes:
                        documentation.Notes = content.ToString();
                        break;
                    case DocumentationType.Synopsis:
                        documentation.Synopsis = content.ToString();
                        break;
                }
            }

            return documentation;
        }

        private string ParseAuthor(string line)
        {
            return line.Replace("Author:", "").Trim();
        }

        private int ParseParameter(ref DocumentationComment comment, string line, List<string> lines, int startIndex)
        {
            string parameterName = string.Empty;
            string description = string.Empty;
            int endIndex = startIndex;

            parameterName = line.Replace(".PARAMETER", "").Trim();

            for (var i = startIndex + 1; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("."))
                    break;

                description += lines[i].Trim() + "\r\n";
                endIndex++;
            }

            comment.Parameters.Add(parameterName, description.Trim());

            return endIndex;
        }
    }
}
