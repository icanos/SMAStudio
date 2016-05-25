using SMAStudiovNext.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SMAStudiovNext.Utils;
using SMAStudiovNext.Core.Editor.Snippets;

namespace SMAStudiovNext.Agents
{
    /// <summary>
    /// Takes care of loading snippets from our snippets cache
    /// </summary>
    public class SnippetsAgent : IAgent
    {
        private readonly ISnippetsCollection _snippetsCollection;

        public SnippetsAgent()
        {
            _snippetsCollection = AppContext.Resolve<ISnippetsCollection>();
        }

        public void Start()
        {
            if (!Directory.Exists(Path.Combine(AppHelper.CachePath, "Snippets")))
            {
                Directory.CreateDirectory(Path.Combine(AppHelper.CachePath, "Snippets"));
                CreateSampleSnippets();
            }

            var files = Directory.GetFiles(Path.Combine(AppHelper.CachePath, "Snippets"), "*.snippet");

            foreach (var file in files)
            {
                var reader = new StreamReader(file);
                string snippetContent = reader.ReadToEnd();
                reader.Close();

                var snippet = new CodeSnippet();
                snippet.Name = new FileInfo(file).Name.Replace(".snippet", "");
                snippet.Text = snippetContent;
                
                _snippetsCollection.Snippets.Add(snippet);
            }
        }

        public void Stop()
        {
            // Nothing
        }

        private void CreateSampleSnippets()
        {
            var textWriter = new StreamWriter(Path.Combine(AppHelper.CachePath, "Snippets", "paramblock.snippet"));
            textWriter.Write("Param(\r\n\t[${Type}]$${ParameterName}\r\n)");

            textWriter.Close();

            /*textWriter = new StreamWriter(Path.Combine(AppHelper.CachePath, "Snippets", "if.snippet"));
            textWriter.Write("if (${true}) {\r\n\r\n}");

            textWriter.Close();*/

            textWriter = new StreamWriter(Path.Combine(AppHelper.CachePath, "Snippets", "foreach.snippet"));
            textWriter.Write("foreach (${variable} in ${collection}) {\r\n\r\n}");

            textWriter.Close();

            textWriter = new StreamWriter(Path.Combine(AppHelper.CachePath, "Snippets", "comment.snippet"));
            textWriter.Write("<#\r\n" +
                ".SYNOPSIS\r\n" +
                "\t${Enter synopsis here}\r\n" +
                ".DESCRIPTION\r\n" +
                "\t${Description}\r\n" +
                ".PARAMETER Parameter1\r\n" +
                "\t${Parameter description}\r\n" +
                ".EXAMPLE\r\n" +
                "\t${Example}\r\n" +
                ".NOTES\r\n" +
                "Author: ${Author}\r\n" +
                "#>");

            textWriter.Close();
        }
    }
}
