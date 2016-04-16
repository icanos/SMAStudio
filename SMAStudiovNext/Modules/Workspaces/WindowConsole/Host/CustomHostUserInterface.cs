using SMAStudiovNext.Modules.WindowConsole.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.WindowConsole.Host
{
    internal class CustomHostUserInterface : PSHostUserInterface, IHostUISupportsMultipleChoiceSelection
    {
        private readonly CustomHostRawUserInterface _rawUserInterface;
        private readonly ConsoleView _consoleView;

        private int _currentOffset = 0;

        public CustomHostUserInterface(ConsoleView consoleView)
        {
            _rawUserInterface = new CustomHostRawUserInterface(consoleView);
            _consoleView = consoleView;
        }

        public override PSHostRawUserInterface RawUI
        {
            get
            {
                return _rawUserInterface;
            }
        }

        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            Write(ConsoleColor.Blue, ConsoleColor.Black, caption + "\r\n" + message + ": ");

            var results = new Dictionary<string, PSObject>();

            foreach (FieldDescription fd in descriptions)
            {
                string[] label = GetHotkeyAndLabel(fd.Label);
                WriteLine(label[1]);

                string userData = ReadLine();
                if (userData == null)
                {
                    return null;
                }

                results[fd.Name] = PSObject.AsPSObject(userData);
            }

            return results;
        }

        public Collection<int> PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, IEnumerable<int> defaultChoices)
        {
            // Write the caption and message strings in Blue.
            this.WriteLine(ConsoleColor.Blue,
                           ConsoleColor.Black,
                           caption + "\n" + message + "\n");

            // Convert the choice collection into something that's a
            // little easier to work with
            // See the BuildHotkeysAndPlainLabels method for details.
            string[,] promptData = BuildHotkeysAndPlainLabels(choices);

            // Format the overall choice prompt string to display...
            StringBuilder sb = new StringBuilder();
            for (int element = 0; element < choices.Count; element++)
            {
                sb.Append(String.Format(CultureInfo.CurrentCulture,
                                        "|{0}> {1} ",
                                        promptData[0, element],
                                        promptData[1, element]));
            }

            Collection<int> defaultResults = new Collection<int>();
            if (defaultChoices != null)
            {
                int countDefaults = 0;
                foreach (int defaultChoice in defaultChoices)
                {
                    ++countDefaults;
                    defaultResults.Add(defaultChoice);
                }

                if (countDefaults != 0)
                {
                    sb.Append(countDefaults == 1 ? "[Default choice is " : "[Default choices are ");
                    foreach (int defaultChoice in defaultChoices)
                    {
                        sb.AppendFormat(CultureInfo.CurrentCulture,
                                        "\"{0}\",",
                                        promptData[0, defaultChoice]);
                    }

                    sb.Remove(sb.Length - 1, 1);
                    sb.Append("]");
                }
            }

            this.WriteLine(ConsoleColor.Cyan, ConsoleColor.Black, sb.ToString());

            // loop reading prompts until a match is made, the default is
            // chosen or the loop is interrupted with ctrl-C.
            Collection<int> results = new Collection<int>();
            while (true)
            {
                ReadNext:
                string prompt = string.Format(CultureInfo.CurrentCulture, "Choice[{0}]:", results.Count);
                this.Write(ConsoleColor.Cyan, ConsoleColor.Black, prompt);
                string data = ReadLine().Trim().ToUpper(CultureInfo.CurrentCulture);

                // if the choice string was empty, no more choices have been made.
                // if there were no choices made, return the defaults
                if (data.Length == 0)
                {
                    return (results.Count == 0) ? defaultResults : results;
                }

                // see if the selection matched and return the
                // corresponding index if it did...
                for (int i = 0; i < choices.Count; i++)
                {
                    if (promptData[0, i] == data)
                    {
                        results.Add(i);
                        goto ReadNext;
                    }
                }

                this.WriteErrorLine("Invalid choice: " + data);
            }
        }

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            // Write the caption and message strings in Blue.
            this.WriteLine(ConsoleColor.Blue,
                           ConsoleColor.Black,
                           caption + "\n" + message + "\n");

            // Convert the choice collection into something that's a
            // little easier to work with
            // See the BuildHotkeysAndPlainLabels method for details.
            string[,] promptData = BuildHotkeysAndPlainLabels(choices);

            // Format the overall choice prompt string to display...
            StringBuilder sb = new StringBuilder();
            for (int element = 0; element < choices.Count; element++)
            {
                sb.Append(String.Format(CultureInfo.CurrentCulture,
                                        "|{0}> {1} ",
                                        promptData[0, element],
                                        promptData[1, element]));
            }

            sb.Append(String.Format(CultureInfo.CurrentCulture,
                                    "[Default is ({0}]",
                                    promptData[0, defaultChoice]));

            // loop reading prompts until a match is made, the default is
            // chosen or the loop is interrupted with ctrl-C.
            while (true)
            {
                this.WriteLine(ConsoleColor.Cyan, ConsoleColor.Black, sb.ToString());
                string data = ReadLine().Trim().ToUpper(CultureInfo.CurrentCulture);

                // if the choice string was empty, use the default selection
                if (data.Length == 0)
                {
                    return defaultChoice;
                }

                // see if the selection matched and return the
                // corresponding index if it did...
                for (int i = 0; i < choices.Count; i++)
                {
                    if (promptData[0, i] == data)
                    {
                        return i;
                    }
                }

                this.WriteErrorLine("Invalid choice: " + data);
            }
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            throw new NotImplementedException(
              "The method PromptForCredential() is not implemented by CustomHost yet.");
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            throw new NotImplementedException(
              "The method PromptForCredential() is not implemented by CustomHost yet.");
        }

        public override string ReadLine()
        {
            var lineInput = _consoleView.GetInput(_currentOffset);

            return lineInput;
        }

        public override SecureString ReadLineAsSecureString()
        {
            throw new NotImplementedException("SecureString not implemented yet.");
        }

        public override void Write(string value)
        {
            _currentOffset = _consoleView.Write(value);
        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            _currentOffset = _consoleView.Write(value, foregroundColor, backgroundColor);
        }

        public override void WriteDebugLine(string message)
        {
            _currentOffset = _consoleView.Write(String.Format(CultureInfo.CurrentCulture, "DEBUG: {0}", message), ConsoleColor.DarkYellow, ConsoleColor.Black);
        }

        public override void WriteErrorLine(string value)
        {
            _currentOffset = _consoleView.Write(value, ConsoleColor.Red, ConsoleColor.Black);
        }

        public override void WriteLine(string value)
        {
            _currentOffset = _consoleView.Write(value + "\r\n");
        }

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            // Nothing yet
        }

        public override void WriteVerboseLine(string message)
        {
            _currentOffset = _consoleView.Write(String.Format(CultureInfo.CurrentCulture, "VERBOSE: {0}", message), ConsoleColor.Green, ConsoleColor.Black);
        }

        public override void WriteWarningLine(string message)
        {
            _currentOffset = _consoleView.Write(String.Format(CultureInfo.CurrentCulture, "WARNING: {0}", message), ConsoleColor.Yellow, ConsoleColor.Black);
        }

        /// <summary>
        /// Parse a string containing a hotkey character.
        /// Take a string of the form
        ///    Yes to &amp;all
        /// and returns a two-dimensional array split out as
        ///    "A", "Yes to all".
        /// </summary>
        /// <param name="input">The string to process</param>
        /// <returns>
        /// A two dimensional array containing the parsed components.
        /// </returns>
        private static string[] GetHotkeyAndLabel(string input)
        {
            string[] result = new string[] { String.Empty, String.Empty };
            string[] fragments = input.Split('&');
            if (fragments.Length == 2)
            {
                if (fragments[1].Length > 0)
                {
                    result[0] = fragments[1][0].ToString().
                    ToUpper(CultureInfo.CurrentCulture);
                }

                result[1] = (fragments[0] + fragments[1]).Trim();
            }
            else
            {
                result[1] = input;
            }

            return result;
        }

        /// <summary>
        /// This is a private worker function splits out the
        /// accelerator keys from the menu and builds a two
        /// dimentional array with the first access containing the
        /// accelerator and the second containing the label string
        /// with the &amp; removed.
        /// </summary>
        /// <param name="choices">The choice collection to process</param>
        /// <returns>
        /// A two dimensional array containing the accelerator characters
        /// and the cleaned-up labels</returns>
        private static string[,] BuildHotkeysAndPlainLabels(
            Collection<ChoiceDescription> choices)
        {
            // we will allocate the result array
            string[,] hotkeysAndPlainLabels = new string[2, choices.Count];

            for (int i = 0; i < choices.Count; ++i)
            {
                string[] hotkeyAndLabel = GetHotkeyAndLabel(choices[i].Label);
                hotkeysAndPlainLabels[0, i] = hotkeyAndLabel[0];
                hotkeysAndPlainLabels[1, i] = hotkeyAndLabel[1];
            }

            return hotkeysAndPlainLabels;
        }
    }
}
