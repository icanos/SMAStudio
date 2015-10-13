using ICSharpCode.AvalonEdit.Document;
using SMAStudio.Editor.CodeCompletion.DataItems;
using SMAStudio.Models;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudio.Editor.CodeCompletion
{
    /*public class PowershellCompletionEngine
    {
        private TextDocument _document;
        private IProjectContent _projectContent;
        private int _offset;
        private TextLocation _location;

        public PowershellCompletionEngine(TextDocument document, IProjectContent projectContent)
        {
            _document = document;
            _projectContent = projectContent;
        }

        public bool TryGetCompletionWord(int offset, out int startPos, out int wordLength)
        {
            startPos = wordLength = 0;
            int pos = offset - 1;

            while (pos >= 0)
            {
                char c = _document.GetCharAt(pos);
                if (!char.IsLetterOrDigit(c) && c != '$' && c != '-')
                    break;
                pos--;
            }

            if (pos == -1)
                return false;

            pos++;
            startPos = pos;

            while (pos < _document.TextLength)
            {
                char c = _document.GetCharAt(pos);
                if (!char.IsLetterOrDigit(c) && c != '$' && c != '-')
                    break;
                pos++;
            }

            wordLength = pos - startPos;
            return true;
        }

        public IEnumerable<CompletionData> GetCompletionData(int offset, bool controlSpace)
        {
            SetOffset(offset);

            if (offset > 0)
            {
                char lastChar = _document.GetCharAt(offset - 1);
                bool isComplete = false;
                var result = MagicKeyCompletion(lastChar, controlSpace, out isComplete) ?? Enumerable.Empty<CompletionData>();

                if (!isComplete && controlSpace && char.IsWhiteSpace(lastChar))
                {
                    //offset -= 2;
                    while (offset >= 0 && char.IsWhiteSpace(_document.GetCharAt(offset)))
                    {
                        offset--;
                    }

                    if (offset > 0)
                    {
                        var nonWhitspaceResult = MagicKeyCompletion(_document.GetCharAt(offset), controlSpace, out isComplete);

                        if (nonWhitspaceResult != null)
                        {
                            var text = new HashSet<string>(result.Select(r => r.CompletionText));
                            result = result.Concat(nonWhitspaceResult.Where(r => !text.Contains(r.CompletionText)));
                        }
                    }
                }

                return result;
            }

            return Enumerable.Empty<CompletionData>();
        }

        public void SetOffset(int offset)
        {
            //Reset();

            _offset = offset;
            _location = _document.GetLocation(offset);
        }

        private IEnumerable<CompletionData> MagicKeyCompletion(char completionChar, bool controlSpace, out bool isComplete)
        {
            Token token;

            Token[] tokens;
            ParseError[] errors;

            Parser.ParseInput(_document.Text, out tokens, out errors);

            token = tokens.FirstOrDefault(t => _offset >= t.Extent.StartOffset && _offset <= t.Extent.EndOffset && t.Kind != TokenKind.NewLine);

            isComplete = false;

            switch (completionChar)
            {
                case '\'':
                case '"':
                    return Enumerable.Empty<CompletionData>();

                // Posh variable
                case '$':
                    if (controlSpace)
                    {
                        var text = GetMemberTextToCaret();
                        return HandleVariableReferenceCompletion(tokens, text);
                    }

                    return HandleVariableReferenceCompletion(tokens, token);

                // Cmdlet parameter
                case '-':
                    // Handles enumeration of approperiate parameters for a CommandName
                    return HandleParameterCompletion(tokens, token, controlSpace);

                // .NET namespace
                case '[':
                    break;
                
                // .NET types
                case ':':
                    break;

                // .NET method/objects
                case '.':
                    break;

                case ' ':
                    var previousToken = GetPreviousToken(tokens, token);

                    if (previousToken.Kind == TokenKind.Parameter)
                    {
                        // TODO: Handle data type completion eg. bool values
                    }
                    else
                    {
                        if (previousToken.TokenFlags != TokenFlags.CommandName)
                        {
                            var commandToken = GetPreviousToken(tokens, token, TokenFlags.CommandName);

                            if (commandToken == null)
                                return null;

                            return HandleParameterCompletion(tokens, commandToken, controlSpace);
                        }
                        else
                            return HandleParameterCompletion(tokens, previousToken, controlSpace);
                    }
                    break;

                default:

                    char prevCh = _offset > 2 ? _document.GetCharAt(_offset - 2) : ';';
                    char nextCh = _offset < _document.TextLength ? _document.GetCharAt(_offset) : ' ';*/
					//const string allowedChars = ";,.[](){}+-*/%^?:&|~!<>=";
    /*
					if ((!Char.IsWhiteSpace(nextCh) && allowedChars.IndexOf(nextCh) < 0) || !(Char.IsWhiteSpace(prevCh) || allowedChars.IndexOf(prevCh) >= 0)) {
						if (!controlSpace)
							return null;
					}

                    return AddContextCompletion(tokens, token, controlSpace);
            }

            return null;
        }

        private IEnumerable<CompletionData> AddContextCompletion(Token[] tokens, Token node, bool controlSpace)
        {
            if (node == null && !controlSpace)
                return null;

            var result = new List<CompletionData>();

            // We only want variables that can be used at this point in the script
            result.AddRange(HandleVariableReferenceCompletion(tokens, node));

            result.AddRange(HandleCmdletCompletion(node, controlSpace));

            result.AddRange(HandleRunbookCompletion(node, controlSpace));

            return result;
        }

        private IEnumerable<CompletionData> HandleParameterCompletion(Token[] tokens, Token node, bool controlSpace)
        {
            // It doesn't matter if we're here on controlSpace or not if node is set to null
            if (node == null)
                return null;

            var result = new List<CompletionData>();

            //var tokenList = tokens.ToList();
            //int tokenIndex = tokenList.IndexOf(node);

            var commandToken = node.TokenFlags == TokenFlags.CommandName ? node : GetPreviousToken(tokens, null, TokenFlags.CommandName); //FindCommandToken(tokens, tokenIndex);
            if (commandToken == null)
                return null;

            // check if this is a cmdlet or runbook
            bool isRunbook = true;

            var componentsViewModel = Core.Resolve<IComponentsViewModel>();
            var runbook = componentsViewModel.Runbooks.Where(r => r.RunbookName.Equals(commandToken.Text, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            CmdletConfigurationEntry cmdlet = null;

            if (runbook == null)
            {
                isRunbook = false;
                cmdlet = _projectContent.Cmdlets.Where(c => c.Name.Equals(commandToken.Text, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            }

            if (isRunbook)
            {
                if (runbook == null)
                    return null;

                // Since this is a runbook, we actually need to parse through it in order 
                // to find any specified in parameters
                Token[] runbookTokens;
                ParseError[] runbookErrors;

                string runbookContent = String.IsNullOrEmpty(runbook.Content) ? runbook.GetContent() : runbook.Content;
                var scriptBlock = Parser.ParseInput(runbookContent, out runbookTokens, out runbookErrors);

                // There is some parse errors in the runbook - we can't scan it!
                if (runbookErrors.Length > 0)
                    return null;

                var paramBlock = FindParametersBlock(scriptBlock);
                if (paramBlock == null)
                    return null;

                List<ParameterAst> parameters = null;

                if (node.TokenFlags != TokenFlags.CommandName)
                {
                    parameters = paramBlock.Parameters.Where(p =>
                        p.Extent.Text.Substring(1).StartsWith(node.Text.Substring(1), StringComparison.InvariantCultureIgnoreCase)).ToList();
                }
                else
                {
                    parameters = paramBlock.Parameters.ToList();
                }

                foreach (var param in parameters)
                {
                    //result.Add(new ParameterCompletionData(param, (node.TokenFlags == TokenFlags.CommandName)));
                }
            }
            else
            {
                // Its a cmdlet
                var properties = cmdlet.ImplementingType.GetProperties();

                foreach (var property in properties)
                {
                    if (!property.CanWrite || property.Name.Equals("CommandRuntime"))
                        continue;

                    //result.Add(new ParameterCompletionData(property, (node.TokenFlags == TokenFlags.CommandName)));
                }
            }

            return result;
        }

        private IEnumerable<CompletionData> HandleCmdletCompletion(Token node, bool controlSpace)
        {
            if (node == null && !controlSpace)
                return null;

            var result = new List<CompletionData>();
            System.Collections.Generic.List<CmdletConfigurationEntry> cmdlets = null;

            if (node != null)
                cmdlets = _projectContent.Cmdlets.Where(c => c.Name.StartsWith(node.Text, StringComparison.InvariantCultureIgnoreCase)).ToList();
            else
                cmdlets = _projectContent.Cmdlets.ToList();

            foreach (var cmdlet in cmdlets)
            {
                result.Add(new CmdletCompletionData(cmdlet));
            }

            return result;
        }

        private IEnumerable<CompletionData> HandleRunbookCompletion(Token node, bool controlSpace)
        {
            if (node == null && !controlSpace)
                return null;

            var result = new List<CompletionData>();
            var componentsViewModel = Core.Resolve<IComponentsViewModel>();

            List<RunbookViewModel> runbooks = null;

            if (node != null)
                runbooks = componentsViewModel.Runbooks.Where(r => r.RunbookName.StartsWith(node.Text, StringComparison.InvariantCultureIgnoreCase)).ToList();
            else
                runbooks = componentsViewModel.Runbooks.ToList();

            foreach (var runbook in componentsViewModel.Runbooks)
            {
                result.Add(new RunbookCompletionData(runbook.Runbook));
            }

            return result;
        }

        private IEnumerable<CompletionData> HandleVariableReferenceCompletion(Token[] tokens, Token node)
        {
            Debug.WriteLine("Code completion for variable part '{0}'", node.Text);
            List<CompletionData> result = new List<CompletionData>();

            List<Token> variables = null;

            if (node != null)
            {
                variables = tokens.Where(t =>
                    t.Kind == TokenKind.Variable
                    && t.Extent.EndOffset < node.Extent.StartOffset
                    && t.Text.StartsWith(node.Text, StringComparison.InvariantCultureIgnoreCase)).ToList();
            }
            else
            {
                variables = tokens.Where(t =>
                    t.Kind == TokenKind.Variable).ToList();
            }

            List<string> usedVariables = new List<string>();
            foreach (var variable in variables)
            {
                if (!usedVariables.Contains(variable.Text))
                {
                    result.Add(new VariableCompletionData(variable, null));
                    usedVariables.Add(variable.Text);
                }
            }

            usedVariables.Clear();

            return result;
        }

        private IEnumerable<CompletionData> HandleVariableReferenceCompletion(Token[] tokens, string text)
        {
            Debug.WriteLine("Code completion for variable part '{0}'", text);
            List<CompletionData> result = new List<CompletionData>();

            var variables =
                tokens.Where(t =>
                    t.Text.StartsWith(text, StringComparison.InvariantCultureIgnoreCase)).ToList();

            foreach (var variable in variables)
            {
                result.Add(new VariableCompletionData(variable, null));
            }

            return result;
        }

        #region Helper methods
        
        protected string GetMemberTextToCaret()
        {
            int stopOffset = _offset;

            while (stopOffset > 0)
            {
                char ch = _document.GetCharAt(stopOffset);
                if (ch == ' ' || ch == '\t')
                {
                    stopOffset++;
                    break;
                }

                --stopOffset;
            }

            return _document.GetText(stopOffset, _offset - stopOffset);
        }

        /// <summary>
        /// Returns the token right before the provided token
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        /// <param name="token">Token to use as start point</param>
        /// <returns>Token or null</returns>
        private Token GetPreviousToken(Token[] tokens, Token token)
        {
            var tokenList = tokens.ToList();
            int tokenOffset = tokenList.IndexOf(token);

            if (token == null)
            {
                var calculatedTokens = tokenList.Where(t => _offset >= t.Extent.EndOffset).ToList();
                token = calculatedTokens[calculatedTokens.Count - 1];

                return token;
            }

            if ((tokenOffset - 1) > 0)
                return tokenList[tokenOffset - 1];

            return null;
        }

        /// <summary>
        /// Returns the previous token (the one before that we pass in to the function)
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        /// <param name="token">Token to find the previous token for</param>
        /// <param name="tokenType">Type of token we're looking for if <see cref="token"/> is null</param>
        /// <returns>Previous token</returns>
        private Token GetPreviousToken(Token[] tokens, Token token, TokenFlags tokenType)
        {
            var tokenList = tokens.ToList();
            int tokenOffset = tokenList.IndexOf(token);

            if (token == null)
            {
                var calculatedTokens = tokenList.Where(t => _offset >= t.Extent.EndOffset).ToList();

                // If we are about to add a value to a parameter, we do not want to show the code complete
                // window with suggestions of other parameters...
                if (calculatedTokens[calculatedTokens.Count - 1].Kind == TokenKind.Parameter)
                    return null;

                for (int i = calculatedTokens.Count - 1; i >= 0; i--)
                {
                    if (calculatedTokens[i].TokenFlags == tokenType)
                        return calculatedTokens[i];
                }
            }

            if ((tokenOffset - 1) > 0)
                return tokenList[tokenOffset - 1];

            return null;
        }

        /// <summary>
        /// Tries to find a ParamBlockAst in the parsed scriptBlock. This is used to extract
        /// the parameters for the script from (context aware completion)
        /// </summary>
        /// <param name="scriptBlock">Script to extract parameters from</param>
        /// <returns>ParamBlockAst or null</returns>
        private ParamBlockAst FindParametersBlock(ScriptBlockAst scriptBlock)
        {
            var statementsToScan = new List<FunctionDefinitionAst>();

            // Scan the BeginBlock
            if (scriptBlock.BeginBlock != null && scriptBlock.BeginBlock.Statements != null)
            {
                foreach (var stmt in scriptBlock.BeginBlock.Statements)
                {
                    if (stmt is FunctionDefinitionAst)
                    {
                        statementsToScan.Add((FunctionDefinitionAst)stmt);
                    }
                }
            }

            // Scan the EndBlock
            if (scriptBlock.EndBlock != null && scriptBlock.EndBlock.Statements != null)
            {
                foreach (var stmt in scriptBlock.EndBlock.Statements)
                {
                    if (stmt is FunctionDefinitionAst)
                    {
                        statementsToScan.Add((FunctionDefinitionAst)stmt);
                    }
                }
            }

            // Go through each StatementAst until we find a ParamBlockAst
            foreach (var stmt in statementsToScan)
            {
                if (stmt.Body.ParamBlock != null && stmt.Body.ParamBlock.Parameters.Count > 0)
                {
                    return stmt.Body.ParamBlock;
                }
            }

            return null;
        }
        #endregion
    }*/
}
