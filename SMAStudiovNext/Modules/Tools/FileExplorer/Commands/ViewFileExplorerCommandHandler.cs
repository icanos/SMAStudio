using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Framework.Threading;
using SMAStudiovNext.Commands;
using SMAStudiovNext.Modules.Tools.FileExplorer.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.Tools.FileExplorer.Commands
{
    [CommandHandler]
    public class ViewFileExplorerCommandHandler : CommandHandlerBase<ViewFileExplorerCommandDefinition>
    {
        private readonly IShell _shell;

        [ImportingConstructor]
        public ViewFileExplorerCommandHandler(IShell shell)
        {
            _shell = shell;
        }

        public override Task Run(Command command)
        {
            _shell.ShowTool<FileExplorerViewModel>();
            return TaskUtility.Completed;
        }
    }
}
