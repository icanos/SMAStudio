using Caliburn.Micro;
using Gemini.Framework.Services;
using SMAStudiovNext.Icons;
using SMAStudiovNext.Modules.Tools.FileExplorer.Models;
using SMAStudiovNext.Modules.Tools.FileExplorer.ViewModels;
using SMAStudiovNext.Modules.Workspaces.WindowScript.ViewModels;
using SMAStudiovNext.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SMAStudiovNext.Modules.Tools.FileExplorer.Commands
{
    public class LoadFileCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (!(parameter is ResourceContainer))
                return false;

            var model = (parameter as ResourceContainer).Tag;

            if (!(model is FileBrowseLink))
                return false;

            var path = model as FileBrowseLink;

            if (path.Path.StartsWith("-"))
                return false;

            return true;
        }

        public void Execute(object parameter)
        {
            var container = parameter as ResourceContainer;
            var path = container.Tag as FileBrowseLink;
            
            var fileInfo = new FileInfo(path.Path);
            
            if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
            {
                // We are opening a new folder
                path.FileExplorer.Items.Clear();

                var backUpPath = new DirectoryInfo(Path.Combine(path.Path, ".."));
                var backUp = new ResourceContainer("..", new FileBrowseLink(backUpPath.FullName, path.FileExplorer));
                path.FileExplorer.Items.Add(backUp);

                var dirs = default(string[]);
                try
                {
                    dirs = Directory.GetDirectories(path.Path);
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Access denied.", "Error");

                    path = new FileBrowseLink(backUpPath.FullName, path.FileExplorer);
                    dirs = Directory.GetDirectories(backUpPath.FullName);
                }

                foreach (var dir in dirs)
                {
                    var dirInfo = new DirectoryInfo(dir);

                    var resource = new ResourceContainer(dirInfo.Name, new FileBrowseLink(dir, path.FileExplorer), IconsDescription.Folder);
                    path.FileExplorer.Items.Add(resource);

                    dirInfo = null;
                }

                var files = Directory.GetFiles(path.Path);

                foreach (var file in files)
                {
                    var fi = new FileInfo(file);
                    if (!FileExplorerViewModel.ValidFileTypes.Contains(fi.Extension))
                        continue;

                    var resource = new ResourceContainer(fi.Name, new FileBrowseLink(file, path.FileExplorer), IconsDescription.Runbook);
                    path.FileExplorer.Items.Add(resource);

                    fileInfo = null;
                }
            }
            else
            {
                // File to open!
                var shell = IoC.Get<IShell>();
                shell.OpenDocument(new ScriptViewModel());
            }
        }
    }
}
