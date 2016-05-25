using Novacode;
using SMAStudiovNext.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using SMAStudiovNext.Modules.WindowRunbook.ViewModels;
using VisioDrawing = VisioAutomation.VDX.Elements.Drawing;
using VisioFace = VisioAutomation.VDX.Elements.Face;
using VisioLine = VisioAutomation.VDX.Elements.Line;
using VisioPage = VisioAutomation.VDX.Elements.Page;
using VisioShape = VisioAutomation.VDX.Elements.Shape;
using VisioTemplate = VisioAutomation.VDX.Template;
using SMAStudiovNext.Core.Editor.Parser;

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
        private readonly IStatusManager _statusManager;

        public DocumentationManager(IBackendContext backendContext)
        {
            _backendContext = backendContext;
            _statusManager = AppContext.Resolve<IStatusManager>();
        }

        public void Dispose()
        {
            
        }

        public void Generate(string templatePath, string documentOutputPath, string drawingOutputPath)
        {
            var languageContext = new LanguageContext();

            // Generate a word documentation
            GenerateWord(templatePath, documentOutputPath);

            // Generate a visio drawing
            GenerateVisio(drawingOutputPath);
        }

        private void GenerateVisio(string drawingOutputPath)
        {
            var pageWidth = 16.53543307086614;
            var pageHeight = 11.69291338582677;
            var left = 0.1;
            var processWidth = 1.5;
            var processHeight = 1.0;
            var processPaddingHorizontal = 0.4;
            var processPaddingVertical = 1.4;
            var maxProcessCountPerRow = 10;
            var activityPos = 1;
            var activityYPos = 0;
            var addToHorizontal = 1;
            var shapes = new List<VisioShape>();

            // Clean up the template and remove the existing pages
            var templateXml = XDocument.Load("DrawingTemplate.vdx");
            //VisioTemplate.CleanUpTemplate(templateXml);

            var template = new VisioTemplate(templateXml.ToString());
            var document = new VisioDrawing(template);
            document.DocumentProperties.Creator = "Automation Studio";
            document.DocumentProperties.TimeCreated = DateTime.Now;

            // Configure the page
            var runbooks = _backendContext.Runbooks;
            var runbooksList = new List<RunbookModelProxy>();
            foreach (var runbook in runbooks)
                runbooksList.Add((runbook.Tag as RunbookModelProxy));

            var languageContext = new LanguageContext();

            foreach (var runbook in runbooks)
            {
                if (!(runbook.Tag as RunbookModelProxy).PublishedRunbookVersionID.HasValue)
                    continue;

                var runbookProxy = (runbook.Tag as RunbookModelProxy);
                _statusManager.SetText("Generating visio drawing for " + runbookProxy.RunbookName);
                var viewModel = (runbook.Tag as RunbookModelProxy).GetViewModel<RunbookViewModel>();

                var page = ConfigureVisioPage(viewModel.Runbook.RunbookName, pageWidth, pageHeight);
                document.Pages.Add(page);

                var publishedContent = viewModel.GetContent(RunbookType.Published, true);
                languageContext.Parse(publishedContent);

                var references = languageContext.GetReferences(runbooksList);

                if (references.Count == 0)
                    continue;

                var totalRows = (double)(runbooksList.Count / maxProcessCountPerRow);

                if (runbooksList.Count <= maxProcessCountPerRow)
                    totalRows = 2.0;

                var y = (pageHeight / totalRows) + (totalRows * ((processHeight + processPaddingVertical)) / totalRows);
                var x = (pageWidth / (maxProcessCountPerRow - 1)) + ((processWidth + processPaddingHorizontal) * activityPos);
                var ay = y - ((processHeight + processPaddingVertical) * activityYPos);

                // Add the current runbook as the start activity
                var processId = document.GetMasterMetaData("Process").ID;
                var visioActivity = new VisioShape(processId, x, ay, processWidth, processHeight);

                var charFormat = new VisioAutomation.VDX.Sections.Char();
                charFormat.Size.Result = 12;

                visioActivity.CharFormats = new List<VisioAutomation.VDX.Sections.Char>();
                visioActivity.CharFormats.Add(charFormat);
                visioActivity.Text.Add(runbookProxy.RunbookName);

                page.Shapes.Add(visioActivity);
                shapes.Add(visioActivity);

                activityPos += addToHorizontal;

                foreach (var reference in references)
                {
                    x = (pageWidth / (maxProcessCountPerRow - 1)) + ((processWidth + processPaddingHorizontal) * activityPos);
                    ay = y - ((processHeight + processPaddingVertical) * activityYPos);

                    // Add all references
                    processId = document.GetMasterMetaData("Process").ID;
                    visioActivity = new VisioShape(processId, x, ay, processWidth, processHeight);

                    visioActivity.CharFormats = new List<VisioAutomation.VDX.Sections.Char>();
                    visioActivity.CharFormats.Add(charFormat);
                    visioActivity.Text.Add(reference.RunbookName);

                    page.Shapes.Add(visioActivity);
                    shapes.Add(visioActivity);

                    /*//if (!String.IsNullOrEmpty(activity.Description))
                    {
                        int shapeID = visioActivity.ID;

                        var callOut = new VisioShape(26, x + 0.3, ay + 1.0);
                        //callOut.Text.Add(activity.Description);
                        callOut.Geom = new VisioAutomation.VDX.Sections.Geom();

                        page.Shapes.Add(callOut);
                    }*/

                    activityPos++;

                    if (activityPos >= maxProcessCountPerRow)
                    {
                        addToHorizontal = -1;
                        activityPos += addToHorizontal;
                        activityYPos++;
                    }
                    else
                    {
                        addToHorizontal = 1;
                        activityPos += addToHorizontal;
                        activityYPos++;
                    }
                }

                // Add connectors between the activities
                for (int i = 0; i < shapes.Count; i++)
                {
                    // We only add connectors up until the latest process object
                    if ((i + 1) >= shapes.Count)
                        break;

                    VisioShape a1 = shapes[i];
                    VisioShape a2 = shapes[i + 1];

                    var line = VisioShape.CreateDynamicConnector(document);
                    line.XForm1D.EndY.Result = 0;

                    line.Line = new VisioLine();
                    line.Line.EndArrow.Result = 2;

                    page.Shapes.Add(line);
                    page.ConnectShapesViaConnector(line, a1, a2);
                }

                shapes.Clear();
                languageContext.ClearCache();
            }

            document.Save(drawingOutputPath);
        }

        private VisioPage ConfigureVisioPage(string documentName, double pageWidth, double pageHeight)
        {
            var page = new VisioPage(pageWidth, pageHeight);
            page.Name = documentName;
            page.PrintProperties.ScaleX.Formula = "1";
            page.PrintProperties.ScaleY.Formula = "1";
            page.PrintProperties.PagesX.Formula = "1";
            page.PrintProperties.PagesY.Formula = "1";
            page.PrintProperties.CenterX.Formula = "0";
            page.PrintProperties.CenterY.Formula = "0";
            page.PrintProperties.OnPage.Formula = "0";
            page.PrintProperties.PrintGrid.Formula = "0";
            page.PrintProperties.PaperKind.Formula = "8";
            page.PrintProperties.PrintPageOrientation.Formula = "2";

            page.PageLayout.PlaceStyle.Formula = "2";
            page.PageLayout.RouteStyle.Formula = "6";
            page.PageLayout.PlaceDepth.Formula = "1";
            page.PageLayout.LineToNodeX.Formula = "0.125";
            page.PageLayout.LineToNodeY.Formula = "0.125";
            page.PageLayout.BlockSizeX.Formula = "0.25";
            page.PageLayout.BlockSizeY.Formula = "0.25";
            page.PageLayout.AvenueSizeX.Formula = "0.5905511811023622";
            page.PageLayout.AvenueSizeY.Formula = "0.5905511811023622";
            page.PageLayout.LineToLineX.Formula = "0.125";
            page.PageLayout.LineToLineY.Formula = "0.125";
            page.PageLayout.LineJumpFactorX.Formula = "0.66666666666667";
            page.PageLayout.LineJumpFactorY.Formula = "0.66666666666667";

            //document.Pages.Add(page);

            return page;
        }

        private void GenerateWord(string templatePath, string documentOutputPath)
        {
            using (var document = DocX.Load(templatePath))
            {
                // Create a new page to start on
                document.InsertSectionPageBreak();

                var runbooks = _backendContext.Runbooks;
                foreach (var runbook in runbooks)
                {
                    if (!(runbook.Tag as RunbookModelProxy).PublishedRunbookVersionID.HasValue)
                        continue;
                    
                    var runbookProxy = (runbook.Tag as RunbookModelProxy);
                    _statusManager.SetText("Generating documentation for " + runbookProxy.RunbookName);
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

                document.SaveAs(documentOutputPath);

                _statusManager.SetTimeoutText("Documentation has successfully been generated.", 5);
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
