using SMAStudio.Editor.CodeCompletion.DataItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml;

namespace SMAStudio.Editor.CodeCompletion.DataItems
{
    class CmdletCompletionData : CompletionData
    {
        private readonly CmdletConfigurationEntry _entity;

        public CmdletConfigurationEntry Entity
        {
            get { return _entity; }
        }

        public CmdletCompletionData(CmdletConfigurationEntry entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            _entity = entity;

            DisplayText = entity.Name;
            CompletionText = entity.Name;

            // TODO: Add Image!
            //Image = 
        }

        private string _description;
        public override string Description
        {
            get
            {
                if (_description == null)
                {
                    _description = DisplayText;
                    _description += Environment.NewLine + XmlDocumentationToText(_entity);
                }

                return _description;
            }
            set
            {
                _description = value;
            }
        }

        public static string XmlDocumentationToText(CmdletConfigurationEntry entity)
        {
            var b = new StringBuilder();

            /*try
            {
                using (XmlTextReader reader = new XmlTextReader(Path.Combine(entity.ImplementingType.Assembly.Location, entity.HelpFileName)))
                //using (XmlTextReader reader = new XmlTextReader(entity.HelpFileName))
                {
                    reader.Read();
                }
            }
            catch (Exception)
            {

            }*/

            return b.ToString();
        }
    }
}
