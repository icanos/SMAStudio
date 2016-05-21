using Gemini.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gemini.Framework.Services;
using System.Collections.ObjectModel;
using SMAStudiovNext.Utils;
using SMAStudiovNext.Services;
using System.IO;
using SMAStudiovNext.Icons;
using System.Windows.Input;
using SMAStudiovNext.Modules.Tools.FileExplorer.Models;
using SMAStudiovNext.Core;
using SMAStudiovNext.Modules.Tools.FileExplorer.Commands;

namespace SMAStudiovNext.Modules.Tools.FileExplorer.ViewModels
{
    [Export(typeof(FileExplorerViewModel))]
    public class FileExplorerViewModel : Tool
    {
        private readonly ObservableCollection<ResourceContainer> _items;
        private readonly ICommand _loadFileCommand;
        public static IList<string> ValidFileTypes = new List<string> { ".ps1", ".psd1", ".psm1", ".txt", ".json", ".xml", ".mof" };

        private string _currentPath = string.Empty;

        public FileExplorerViewModel()
        {
            _items = new ObservableCollection<ResourceContainer>();
            _currentPath = SettingsService.CurrentSettings.LastFileExplorerPath ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _loadFileCommand = new LoadFileCommand();

            Initialize();
        }

        private void Initialize()
        {
            var backUp = new ResourceContainer("..", new FileBrowseLink(Path.Combine(_currentPath, ".."), this));
            Items.Add(backUp);

            // Enumerate folders and files
            var dirs = Directory.GetDirectories(_currentPath);

            // Add them to the file explorer
            foreach (var dir in dirs)
            {
                var dirInfo = new DirectoryInfo(dir);

                var resource = new ResourceContainer(dirInfo.Name, new FileBrowseLink(dir, this), IconsDescription.Folder);
                Items.Add(resource);

                dirInfo = null;
            }

            var files = Directory.GetFiles(_currentPath);

            // Add them to the file explorer
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (!ValidFileTypes.Contains(fileInfo.Extension))
                    continue;

                var resource = new ResourceContainer(fileInfo.Name, new FileBrowseLink(file, this), IconsDescription.Runbook);
                Items.Add(resource);

                fileInfo = null;
            }
        }

        public ICommand LoadCommand
        {
            get
            {
                return _loadFileCommand;
            }
        }

        public override PaneLocation PreferredLocation
        {
            get
            {
                return PaneLocation.Left;
            }
        }

        public ObservableCollection<ResourceContainer> Items
        {
            get { return _items; }
        }

        public override string DisplayName
        {
            get { return "File Explorer"; }
        }
    }
}
