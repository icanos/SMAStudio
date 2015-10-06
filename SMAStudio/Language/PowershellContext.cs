using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Language
{
    public class PowershellContext
    {
        private PowershellParser _parser;
        private List<PowershellSegment> _segments = null;

        private List<string> _keywords = new List<string> { "Begin", "Break", "Catch", "Continue", "Data", "Do", "DynamicParam", "Else", "ElseIf", "End", "Exit", "Filter", "Finally", "For", "ForEach", "From", "Function", "If", "In", "InlineScript", "Hidden", "Parallel", "Param", "Process", "Return", "Sequence", "Switch", "Throw", "Trap", "Try", "Until", "While", "Workflow" };
        private List<string> _standardCmdlets = new List<string>();
        private List<string> _cmdlets = new List<string>();
        private List<string> _cachedModules = new List<string>();

        public PowershellContext()
        {
            _parser = new PowershellParser();
            _parser.IgnoreBlockMarks = true;

            Task.Factory.StartNew(delegate()
            {
                GetCmdlets(0);
            });
        }

        public void SetContent(string content)
        {
            _parser.Clear();
            _segments = _parser.Parse(content);
        }

        /// <summary>
        /// Retrieve the current context of a position in the document
        /// </summary>
        /// <param name="position">Position to find context of</param>
        public List<PowershellSegment> GetContext(int position)
        {
            var context = new List<PowershellSegment>();

            var parts = _segments.Where(s => s.Start <= position).ToList();
            parts.Reverse();

            var inBlockToSkip = false;
            var blockedType = ExpressionType.None;
            foreach (var part in parts)
            {
                if (part.Type == ExpressionType.ExpressionEnd || part.Type == ExpressionType.BlockEnd || part.Type == ExpressionType.TypeEnd)
                {
                    inBlockToSkip = true;
                    blockedType = part.Type;
                }
                else if ((part.Type == ExpressionType.ExpressionStart && blockedType == ExpressionType.ExpressionEnd) ||
                    (part.Type == ExpressionType.BlockStart && blockedType == ExpressionType.BlockEnd) ||
                    (part.Type == ExpressionType.TypeStart && blockedType == ExpressionType.TypeEnd))
                {
                    inBlockToSkip = false;
                    continue;
                }

                if (inBlockToSkip)
                    continue;

                if (part.Type == ExpressionType.Type && _parser.IgnoreBlockMarks)
                    continue;

                context.Add(part);
            }

            if (context.Count > 0 && context[0].Stop < position)
                context.RemoveAt(0);

            Console.WriteLine("Context depth = " + context.Count);

            return context;
        }

        /// <summary>
        /// Return a list of functions defined in the script we're parsing.
        /// </summary>
        /// <returns></returns>
        public List<string> GetFunctions()
        {
            var functions = new List<string>();

            for (int i = 0; i < _segments.Count; i++)
            {
                if (_segments[i].Type == ExpressionType.Function && _segments.Count > i + 1)
                    functions.Add(_segments[i + 1].Value);
            }

            return functions;
        }

        /// <summary>
        /// Return a list of variables defined in the script. This function returns all 
        /// variables found, regardless of scope.
        /// </summary>
        /// <returns></returns>
        public List<string> GetVariables()
        {
            return GetVariables(0);
        }

        /// <summary>
        /// Returns a list of variables "seen" at the contextual position provided. Provide 0
        /// to return all variables, regardless of scope.
        /// </summary>
        /// <param name="contextualPosition"></param>
        /// <returns></returns>
        public List<string> GetVariables(int contextualPosition, string pattern = "")
        {
            List<PowershellSegment> segments = null;// 

            if (contextualPosition == 0)
                segments = _segments.Where(s => s.Type == ExpressionType.Variable && s.Value.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase)).ToList();
            else
                segments = GetContext(contextualPosition).Where(s => s.Type == ExpressionType.Variable && s.Value.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase)).ToList();

            var variables = new List<string>();

            foreach (var segment in segments)
                if (!variables.Contains(segment.Value))
                    variables.Add(segment.Value);

            return variables;
        }

        /// <summary>
        /// Returns a list of parameters bound to a keyword... This is not known yet.
        /// </summary>
        /// <param name="contextualPosition"></param>
        /// <returns></returns>
        public List<string> GetParameters(int contextualPosition)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return all modules to be imported in the script
        /// </summary>
        /// <returns></returns>
        public List<string> GetImportedModules()
        {
            return GetImportedModules(0);
        }

        /// <summary>
        /// Return all modules to be imported in the script
        /// </summary>
        /// <param name="contextualPosition">Position in the script/workflow</param>
        /// <returns></returns>
        public List<string> GetImportedModules(int contextualPosition)
        {
            var modules = new List<string>();

            if (_segments == null)
                return modules;

            List<PowershellSegment> segments = null;// 

            if (contextualPosition == 0)
                segments = _segments.Where(s => s.Type == ExpressionType.Keyword && (s.Value.ToLower().Equals("import-module") || s.Value.ToLower().Equals("ipmo"))).ToList();
            else
                segments = GetContext(contextualPosition).Where(s => s.Type == ExpressionType.Keyword && (s.Value.ToLower().Equals("import-module") || s.Value.ToLower().Equals("ipmo"))).ToList();

            foreach (var mod in segments)
            {
                int pos = _segments.IndexOf(mod);

                for (int i = pos + 1; i < _segments.Count; i++)
                {
                    if (_segments[i].Type == ExpressionType.String || _segments[i].Type == ExpressionType.QuotedString)
                        modules.Add(_segments[i].Value);
                    else
                        break;
                }
            }

            return modules;
        }

        public List<string> GetCmdlets(int contextualPosition, string pattern = "")
        {
            var foundCmdlets = new List<string>();

            // Get all modules that we need to look into as well
            var modules = GetImportedModules(contextualPosition);

            // Standard powershell modules (System32 and Program Files)
            if (_standardCmdlets.Count == 0)
            {
                using (var context = PowerShell.Create())
                {
                    context.AddScript("Get-Command");

                    //var cmdlets = await Task.Factory.FromAsync(context.BeginInvoke(), pResult => context.EndInvoke(pResult));
                    var cmdlets = context.Invoke();

                    foreach (var cmdlet in cmdlets)
                        _standardCmdlets.Add(cmdlet.ToString());

                    Console.WriteLine("Cmdlets = " + cmdlets.Count);
                }
            }

            // Get module cmdlets
            if (!_cachedModules.Equals(modules))
            {
                using (var context = PowerShell.Create())
                {
                    context.AddScript("Import-Module " + String.Join(",", modules) + "; Get-Command -Module " + String.Join(",", modules));

                    //var cmdlets = await Task.Factory.FromAsync(context.BeginInvoke(), pResult => context.EndInvoke(pResult));
                    var cmdlets = context.Invoke();

                    foreach (var cmdlet in cmdlets)
                        _cmdlets.Add(cmdlet.ToString());

                    Console.WriteLine("Cmdlets = " + cmdlets.Count);
                }
            }

            foundCmdlets.AddRange(_standardCmdlets.Where(c => c.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase)).ToList());
            foundCmdlets.AddRange(_cmdlets.Where(c => c.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase)).ToList());

            foundCmdlets.Sort();

            return foundCmdlets;
        }
    }
}
