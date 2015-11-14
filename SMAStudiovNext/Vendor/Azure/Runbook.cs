using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Vendor.Azure
{
    public class Runbook
    {
        private string _state = string.Empty;

        public string Id { get; set; }

        /// <summary>
        /// NOTICE! Randomly generated ID for each time we download the runbook from Azure Automation. Do not store or use
        /// for anything else than keeping track of if this is a draft version of the runbook.
        /// </summary>
        public Guid? DraftRunbookVersionID { get; set; }

        /// <summary>
        /// NOTICE! Randomly generated ID for each time we download the runbook from Azure Automation. Do not store or use
        /// for anything else than keeping track of if this is a published version of the runbook.
        /// </summary>
        public Guid? PublishedRunbookVersionID { get; set; }

        /// <summary>
        /// NOTICE! This is a generated ID in order for us to separate newly created runbooks from runbooks downloaded
        /// from the Azure Web Service. We actually don't need a ID as we'll be using the runbook name when communicating
        /// with Azure Automation.
        /// </summary>
        public Guid RunbookID { get; set; }

        public string RunbookName { get; set; }
        
        public string Tags { get; set; }

        public string State
        {
            get { return _state; }
            set
            {
                if (value.Equals(_state))
                    return;

                _state = value;

                if (_state.Equals("New"))
                {
                    DraftRunbookVersionID = Guid.NewGuid(); // dummy so that SMA Studio knows there is a draft version
                    PublishedRunbookVersionID = null;
                }
                else if (_state.Equals("Edit"))
                {
                    DraftRunbookVersionID = Guid.NewGuid();
                    PublishedRunbookVersionID = Guid.NewGuid();
                }
                else
                {
                    DraftRunbookVersionID = null;
                    PublishedRunbookVersionID = Guid.NewGuid();
                }
            }
        }
    }
}
