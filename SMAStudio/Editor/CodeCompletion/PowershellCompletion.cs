using ICSharpCode.AvalonEdit.Document;
using SMAStudio.Editor.CodeCompletion.DataItems;
using SMAStudio.Models;
using SMAStudio.Services;
using SMAStudio.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Editor.CodeCompletion
{
    /// <summary>
    /// TODO: Implement aliases for parameters and cmdlets
    /// </summary>
    /*public class PowershellCompletion
    {
        //private List<CompletionData> _completionData;
        private IProjectContent _projectContent;

        public PowershellCompletion()
        {
            _projectContent = new PowershellProjectContent();
            //_completionData = new List<CompletionData>();

            Runspace runspace = RunspaceFactory.CreateRunspace();
            var cmdlets = runspace.RunspaceConfiguration.Cmdlets;

            Stopwatch total = Stopwatch.StartNew();
            Parallel.For(
                0,
                cmdlets.Count,
                delegate(int i)
                {
                    var cmdlet = cmdlets[i];
                    _projectContent.AddCmdlet(cmdlet);
                });

            // Add common parameters

            Core.Log.DebugFormat("Init code completion, loading base cmdlets: {0}", total.Elapsed);
        }

        public CodeCompletionResult GetCompletions(TextDocument document, int offset, bool controlSpace, string imports)
        {
            var result = new CodeCompletionResult();

            var pce = new PowershellCompletionEngine(
                document,
                _projectContent);

            var completionChar = document.GetCharAt(offset - 1);
            int startPos, triggerWordLength;
            IEnumerable<ICSharpCode.AvalonEdit.CodeCompletion.ICompletionData> completionData;
            
            if (controlSpace)
            {
                if (!pce.TryGetCompletionWord(offset, out startPos, out triggerWordLength))
                {
                    startPos = offset;
                    triggerWordLength = 0;
                }

                completionData = pce.GetCompletionData(startPos, true);
            }
            else
            {
                startPos = offset;

                if (char.IsLetterOrDigit(completionChar) || completionChar == '$')
                {
                    if (startPos > 1 && char.IsLetterOrDigit(document.GetCharAt(startPos - 2)))
                        return result;

                    completionData = pce.GetCompletionData(startPos, false);
                    startPos--;
                    triggerWordLength = 1;
                }
                else
                {
                    completionData = pce.GetCompletionData(startPos, false);
                    triggerWordLength = 0;
                }
            }

            result.TriggerWordLength = triggerWordLength;
            result.TriggerWord = document.GetText(offset - triggerWordLength, triggerWordLength);
            Debug.WriteLine("Trigger word: {0}", result.TriggerWord);

            foreach (var completion in completionData)
            {
                var poshCompletionData = completion as CompletionData;
                if (poshCompletionData != null)
                {
                    poshCompletionData.TriggerWord = result.TriggerWord;
                    poshCompletionData.TriggerWordLength = result.TriggerWordLength;
                    result.CompletionData.Add(poshCompletionData);
                }
            }

            // Parameter completions for cmdlets
            if (!controlSpace)
            {

            }

            return result;
        }
    }*/
}
