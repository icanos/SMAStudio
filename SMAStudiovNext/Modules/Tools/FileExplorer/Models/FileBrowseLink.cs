using SMAStudiovNext.Modules.Tools.FileExplorer.ViewModels;
using SMAStudiovNext.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.Tools.FileExplorer.Models
{
    public class FileBrowseLink
    {
        private readonly string _path;
        private readonly FileExplorerViewModel _fileExplorer;

        public FileBrowseLink(string path, FileExplorerViewModel fileExplorer)
        {
            _path = path;
            _fileExplorer = fileExplorer;
        }

        public string Path { get { return _path; } }
        
        public FileExplorerViewModel FileExplorer { get { return _fileExplorer; } }
    }
}
