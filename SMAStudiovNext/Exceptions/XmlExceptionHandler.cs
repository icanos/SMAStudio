using Caliburn.Micro;
using Gemini.Modules.Output;
using System.Windows;
using System.Xml;

namespace SMAStudiovNext.Core
{
    public class XmlExceptionHandler
    {
        public static void Show(string xml)
        {
            try
            {
                var document = new XmlDocument();
                document.LoadXml(xml);

                if (document.LastChild != null)
                {
                    string code = "";
                    string message = "";

                    if (document.LastChild.ChildNodes.Count > 0)
                        code = document.LastChild.ChildNodes[0].InnerText;

                    if (document.LastChild.ChildNodes.Count > 1)
                        message = document.LastChild.ChildNodes[1].InnerText;

                    var output = IoC.Get<IOutput>();
                    output.AppendLine("Error: " + message + " (code: " + code + ")");

                    MessageBox.Show("Error: " + message + " (code: " + code + ")");
                }
            }
            catch (XmlException)
            {
                var output = IoC.Get<IOutput>();
                output.AppendLine(xml);
            }
        }

        public static bool IsXml(string content)
        {
            try
            {
                var document = new XmlDocument();
                document.LoadXml(content);

                document = null;

                return true;
            }
            catch (XmlException)
            {
                return false;
            }
        }
    }
}
