using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Framework.Threading;
using SMAStudiovNext.Commands;
using SMAStudiovNext.Modules.PartEnvironmentExplorer.ViewModels;

namespace SMAStudiovNext.Modules.PartEnvironmentExplorer.Commands
{
    [CommandHandler]
    public class ViewEnvironmentExplorerCommandHandler : CommandHandlerBase<ViewEnvironmentExplorerCommandDefinition>
    {
        private readonly IShell _shell;

        [ImportingConstructor]
        public ViewEnvironmentExplorerCommandHandler(IShell shell)
        {
            _shell = shell;
        }

        public override Task Run(Command command)
        {
            _shell.ShowTool<EnvironmentExplorerViewModel>();
            return TaskUtility.Completed;
        }
    }
}
