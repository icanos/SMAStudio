using Gemini.Framework;
using SMAStudiovNext.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace SMAStudiovNext.Modules.Workspaces.WindowScript.ViewModels
{
    public class ScriptViewModel : Document, IViewModel, ICodeViewModel
    {
        public ScriptViewModel()
        {

        }

        public string Content
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public DateTime LastKeyStroke
        {
            get; set;
        }

        public object Model
        {
            get; set;
        }

        public string Name
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool UnsavedChanges
        {
            get; set;
        }

        /// <summary>
        /// Not used
        /// </summary>
        /// <param name="completionWord"></param>
        /// <returns></returns>
        public IList<ICompletionData> GetParameters(string completionWord, bool forceParameterUpdate = false)
        {
            throw new NotImplementedException();
        }
    }
}
