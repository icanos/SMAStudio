using SMAStudio.Analysis;
using SMAStudio.Editor.CodeCompletion.DataItems;
using SMAStudio.Services;
using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SMAStudio.Language
{
    public class PowershellContext
    {
        private PowershellParser _parser;
        private List<PowershellSegment> _segments = null;

        private List<string> _keywords = new List<string>
        {
            "Begin", "Break", "Catch", "Continue", "Data", "Do", "DynamicParam", "Else", "ElseIf", "End", 
            "Exit", "Filter", "Finally", "For", "ForEach", "From", "Function", "If", "In", "InlineScript", 
            "Hidden", "Parallel", "Param", "Process", "Return", "Sequence", "Switch", "Throw", "Trap", "Try", 
            "Until", "While", "Workflow"
        };

        private List<string> _reservedParameters = new List<string>
        {
            "Debug", "ErrorAction", "ErrorVariable", "InformationAction", "InformationVariable", 
            "OutVariable", "OutBuffer", "PiplineVariable", "Verbose", "WarningAction", 
            "WarningVariable", "WhatIf", "Confirm"
        };

        private List<CmdletCompletionData> _standardCmdlets = new List<CmdletCompletionData>();
        private List<CmdletCompletionData> _cmdlets = new List<CmdletCompletionData>();
        private List<string> _cachedModules = new List<string>();

        private object _syncLock = new object();

        private IParameterParserService _parameterParserService;

        public PowershellContext()
        {
            _parser = new PowershellParser();
            _parser.IgnoreBlockMarks = true;

            _parameterParserService = Core.Resolve<IParameterParserService>();

            Task.Factory.StartNew(delegate()
            {
                GetCmdlets(0);
            });
        }

        public void SetContent(string content)
        {
            lock (_syncLock)
            {
                _parser.Clear();
                _segments = _parser.Parse(content);
            }
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

            return context;
        }

        /// <summary>
        /// Get context name of the position provided
        /// </summary>
        /// <param name="contextualPosition">Position to get context from</param>
        /// <returns>Name of the context</returns>
        public ExpressionType GetContextName(int contextualPosition)
        {
            lock (_syncLock)
            {
                var context = _segments.Where(s => s.Start <= contextualPosition).ToList();

                return context[context.Count - 1].Type;
            }
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
        public List<VariableCompletionData> GetVariables()
        {
            return GetVariables(0);
        }

        /// <summary>
        /// Returns a list of variables "seen" at the contextual position provided. Provide 0
        /// to return all variables, regardless of scope.
        /// </summary>
        /// <param name="contextualPosition"></param>
        /// <returns></returns>
        public List<VariableCompletionData> GetVariables(int contextualPosition, string pattern = "")
        {
            List<PowershellSegment> segments = null;//

            if (pattern == null)
                pattern = string.Empty;

            if (contextualPosition == 0)
            {
                segments = _segments
                    .Where(s => s.Type == ExpressionType.Variable && s.Value.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();
            }
            else
            {
                segments = GetContext(contextualPosition)
                    .Where(s => s.Type == ExpressionType.Variable && s.Value.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();
            }

            var variables = new List<VariableCompletionData>();

            foreach (var segment in segments)
            {
                var variableObj = new VariableCompletionData(segment.Value);

                if (!variables.ContainsElement(variableObj))
                    variables.Add(variableObj);
            }

            return variables;
        }

        /// <summary>
        /// Returns a list of parameters bound to a keyword... This is not known yet.
        /// </summary>
        /// <param name="contextualPosition"></param>
        /// <returns></returns>
        public List<ParameterCompletionData> GetParameters(int contextualPosition, string pattern = "")
        {
            // Try to find out the cmdlet we're requesting parameters for
            var context = GetContext(contextualPosition);
            var parsedCmdlet = default(PowershellSegment);

            // An if statement consists of if ([segment1] [segment2] [segment3]) / while ([segment1] [segment2] [segment3])
            bool isOperator = false;

            if (context.Count > 1)
            {
                var segment1 = context[2];
                
                if ((segment1.Type == ExpressionType.LanguageConstruct && segment1.Value.Equals("if", StringComparison.InvariantCultureIgnoreCase)) ||
                    (segment1.Type == ExpressionType.LanguageConstruct && segment1.Value.Equals("while", StringComparison.InvariantCultureIgnoreCase)) ||
                    (segment1.Type == ExpressionType.BlockStart))
                {
                    isOperator = true;
                }
            }

            if (!isOperator)
            {
                foreach (var item in context)
                {
                    if (item.Type == ExpressionType.Keyword)
                    {
                        parsedCmdlet = item;
                        break;
                    }
                }

                if (parsedCmdlet == null)
                    return new List<ParameterCompletionData>();
            }
            else
            {
                // Operator
                var list = new List<ParameterCompletionData>();
                list.AddRange(_parser.Operators.Select(o => new ParameterCompletionData("", o, false, ParameterTypes.LanguageConstruct)).ToList());

                return list;
            }

            var parameters = GetParameters(parsedCmdlet.Value);

            if (!isOperator)
                parameters.AddRange(_reservedParameters.Select(p => new ParameterCompletionData("", p, false, ParameterTypes.LanguageConstruct)).ToList());

            return parameters;
        }

        /// <summary>
        /// Returns a list of parameters for a cmdlet
        /// </summary>
        /// <param name="cmdlet"></param>
        /// <returns></returns>
        public List<ParameterCompletionData> GetParameters(string cmdletStr)
        {
            CmdletCompletionData cmdlet = null;
            var components = Core.Resolve<IEnvironmentExplorerViewModel>();

            lock (_standardCmdlets)
            {
                cmdlet = _standardCmdlets.Where(c => c != null && c.Text.ToLower().Equals(cmdletStr.ToLower())).FirstOrDefault();
            }

            lock (_cmdlets)
            {
                if (cmdlet == null)
                    cmdlet = _cmdlets.Where(c => c != null && c.Text.ToLower().Equals(cmdletStr.ToLower())).FirstOrDefault();
            }

            if (cmdlet == null)
            {
                // Rewrote this to use a parameter scanning service to retrieve this information in the background
                // to speed up the retrieval of information.
                return _parameterParserService.GetParameters(cmdletStr).Select(p => new ParameterCompletionData(
                        p.TypeName,
                        p.Name,
                        false,
                        ParameterTypes.Parameter
                    )).ToList();
            }

            if (cmdlet == null)
                return new List<ParameterCompletionData>();

            if (cmdlet.Parameters.Count == 0)
            {
                using (var context = PowerShell.Create())
                {
                    context.AddScript("Get-Command " + cmdlet.ToString() + " | select -expandproperty parameters");
                    var paramsFromPs = context.Invoke();

                    if (paramsFromPs.Count > 0)
                    {
                        var result = (Dictionary<string, ParameterMetadata>)paramsFromPs[0].BaseObject;

                        lock (cmdlet)
                        {
                            Parallel.ForEach(result.Keys, (key) =>
                            {
                                var paramObj = new ParameterCompletionData(
                                    result[key].ParameterType.Name, 
                                    result[key].Name, 
                                    result[key].ParameterType.Name.Equals("SwitchParameter"),
                                    ParameterTypes.Parameter);

                                if (cmdlet.Parameters.ContainsElement(paramObj))
                                    cmdlet.Parameters.Add(paramObj);
                            });
                        }
                    }
                }

                CacheCmdlets(_standardCmdlets);
            }

            return cmdlet.Parameters.Where(p => p != null).OrderBy(p => p.DisplayText).ToList();
        }

        /// <summary>
        /// Return a list of language constructs ready for auto completions
        /// </summary>
        /// <param name="pattern">Pattern to filter by</param>
        /// <returns>List of language constructs</returns>
        public List<CmdletCompletionData> GetLanguageConstructs(string pattern = "")
        {
            if (pattern == null)
                pattern = string.Empty;

            return _parser.Language
                .Where(l => l.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase))
                .Select(l => new CmdletCompletionData(l, CmdletTypes.Builtin))
                .ToList();
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
            {
                segments = _segments
                    .Where(s => s.Type == ExpressionType.Keyword && (s.Value.ToLower().Equals("import-module") || s.Value.ToLower().Equals("ipmo")))
                    .ToList();
            }
            else
            {
                segments = GetContext(contextualPosition)
                    .Where(s => s.Type == ExpressionType.Keyword && (s.Value.ToLower().Equals("import-module") || s.Value.ToLower().Equals("ipmo")))
                    .ToList();
            }

            foreach (var mod in segments)
            {
                int pos = _segments.IndexOf(mod);

                for (int i = pos + 1; i < _segments.Count; i++)
                {
                    if (_segments[i].Type == ExpressionType.String || _segments[i].Type == ExpressionType.QuotedString)
                    {
                        if (!modules.Contains(_segments[i].Value))
                            modules.Add(_segments[i].Value);
                    }
                    else
                        break;
                }
            }

            return modules;
        }

        /// <summary>
        /// Get a list of cmdlets in the current context (incl. all imported modules)
        /// </summary>
        /// <param name="contextualPosition">Contextual position</param>
        /// <param name="pattern">Pattern to filter by</param>
        /// <returns>List of cmdlets</returns>
        public List<CompletionData> GetCmdlets(int contextualPosition, string pattern = "")
        {
            var foundCmdlets = new List<CompletionData>();

            if (pattern == null)
                pattern = string.Empty;

            // Get all modules that we need to look into as well
            var modules = GetImportedModules(contextualPosition);

            // Standard powershell modules (System32 and Program Files)
            if (_standardCmdlets.Count == 0)
            {
                Core.Resolve<IWorkspaceViewModel>().StatusBarText = "Building code completion cache...";

                if (File.Exists(Path.Combine(AppHelper.CachePath, "data", "cmdlets.xml")))
                {
                    TextReader reader = null;

                    try
                    {
                        reader = (TextReader)new StreamReader(Path.Combine(AppHelper.CachePath, "data", "cmdlets.xml"));

                        var serializer = new XmlSerializer(typeof(List<CmdletCompletionData>));
                        _standardCmdlets = (List<CmdletCompletionData>)serializer.Deserialize(reader);

                        serializer = null;
                    }
                    catch (Exception ex)
                    {
                        Core.Log.Error("Unable to read cmdlet cache", ex);
                        reader.Close();
                    }
                }

                if (_standardCmdlets.Count == 0)
                {
                    using (var context = PowerShell.Create())
                    {
                        context.AddScript("Get-Command");
                        var cmdlets = context.Invoke();

                        lock (_standardCmdlets)
                        {
                            var cache = new List<CmdletCompletionData>();

                            Parallel.ForEach(cmdlets, (cmdlet) =>
                            {
                                var cmdletObj = new CmdletCompletionData(cmdlet.ToString(), CmdletTypes.Custom);

                                if (cmdletObj != null)
                                {
                                    cache.Add(cmdletObj);
                                }
                            });

                            _standardCmdlets = cache;
                        }
                    }
                }

                CacheCmdlets(_standardCmdlets);

                Core.Resolve<IWorkspaceViewModel>().StatusBarText = "Building code completion cache completed.";
            }

            bool hasNewModules = false;
            foreach (var mod in modules)
            {
                if (!_cachedModules.Contains(mod))
                {
                    hasNewModules = true;
                    break;
                }
            }
            
            // Get module cmdlets
            if (hasNewModules)
            {
                try
                {
                    using (var context = PowerShell.Create())
                    {
                        context.AddScript("Import-Module " + String.Join(",", modules) + "; Get-Command -Module " + String.Join(",", modules));

                        var cmdlets = context.Invoke();

                        lock (_cmdlets)
                        {
                            var cache = new List<CmdletCompletionData>();

                            foreach (var cmdlet in cmdlets)
                            {
                                var cmdletObj = new CmdletCompletionData(cmdlet.ToString(), CmdletTypes.Custom);
                                
                                cache.Add(cmdletObj);
                            }

                            _cmdlets = cache;
                        }
                    }
                }
                catch (ParseException)
                {

                }
            }

            // We want to auto complete names of the runbooks too
            var components = Core.Resolve<IEnvironmentExplorerViewModel>();

            lock (_syncLock)
            {
                foundCmdlets.AddRange(_standardCmdlets
                    //.Distinct()
                    .Where(c => c != null && c.Text.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase))
                    .ToList());
                
                foundCmdlets.AddRange(_cmdlets
                    //.Distinct()
                    .Where(c => c != null && c.Text.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase))
                    .ToList());

                foundCmdlets.AddRange(GetLanguageConstructs(pattern));

                foundCmdlets.AddRange(components.Runbooks
                    //.Distinct()
                    .Where(r => r.RunbookName.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase))
                    .Select(r => new RunbookCompletionData(r.Runbook))
                    .ToList());

                foundCmdlets.AddRange(_keywords.Where(k => k.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase))
                    .Select(k => new CmdletCompletionData(k, CmdletTypes.Builtin))
                    .ToList());
            }

            return foundCmdlets;
        }

        /// <summary>
        /// Cache any loaded cmdlets to save us from enumerating painfully slow Powershell commands
        /// </summary>
        /// <param name="cmdlets"></param>
        private void CacheCmdlets(List<CmdletCompletionData> cmdlets)
        {
            // cache this info to disk since this most likely won't change that much over time
            if (!Directory.Exists(Path.Combine(AppHelper.CachePath, "data")))
            {
                Directory.CreateDirectory(Path.Combine(AppHelper.CachePath, "data"));
            }

            lock (_syncLock)
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(List<CmdletCompletionData>));
                    var textWriter = new StreamWriter(Path.Combine(AppHelper.CachePath, "data", "cmdlets.xml"));
                    serializer.Serialize(textWriter, cmdlets);

                    textWriter.Flush();
                    textWriter.Close();
                }
                catch (Exception)
                {
                    // Do nothing!
                }
            }
        }
    }
}
