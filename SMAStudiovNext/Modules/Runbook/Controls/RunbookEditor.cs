using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Xml;

namespace SMAStudiovNext.Modules.Runbook.Controls
{
    /// <summary>
    /// This is our custom implementation of the AvalonEdit editor
    /// to be able to add support for code completion and reference counting etc.
    /// 
    /// This is very much based upon Gemini.Modules.CodeEditor
    /// </summary>
    public class RunbookEditor : TextEditor
    {
        public RunbookEditor()
        {
            FontFamily = new FontFamily("Consolas");
            FontSize = 12;
            ShowLineNumbers = true;
            Options = new TextEditorOptions
            {
                ConvertTabsToSpaces = true
            };
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public override void BeginInit()
        {
            base.BeginInit();

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SMAStudiovNext.Modules.Runbook.SyntaxHighlightning.Powershell.xshd";

            var stream = assembly.GetManifestResourceStream(resourceName);
            var reader = new XmlTextReader(stream);

            SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);

            reader.Close();
            stream.Close();
        }
    }
}
