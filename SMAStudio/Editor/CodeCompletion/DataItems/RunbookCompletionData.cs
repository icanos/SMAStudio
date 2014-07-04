using SMAStudio.Resources;
using SMAStudio.SMAWebService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SMAStudio.Editor.CodeCompletion.DataItems
{
    class RunbookCompletionData : CompletionData
    {
        private readonly Runbook _runbook;

        public RunbookCompletionData(Runbook runbook)
        {
            if (runbook == null)
                throw new ArgumentNullException("runbook");

            _runbook = runbook;

            DisplayText = runbook.RunbookName;
            CompletionText = runbook.RunbookName;
        }

        public override ImageSource Image
        {
            get
            {
                return Icons.GetImage(Icons.Runbook);
            }
            set
            {
                
            }
        }

        private string _description;
        public override string Description
        {
            get
            {
                if (_description == null)
                {
                    _description = DisplayText;
                    _description += Environment.NewLine + _runbook.Description;
                }

                return _description;
            }
            set
            {
                _description = value;
            }
        }
    }
}
